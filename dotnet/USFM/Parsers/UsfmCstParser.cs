using System.Collections.Immutable;
using USFM.Lexers;

namespace USFM.Parsers;

public ref struct UsfmCstParser
{
    private UsfmLexer _lexer;
    private readonly ReadOnlyMemory<char> _source;
    private readonly List<ParsingDiagnostic> _diagnostics;

    public IReadOnlyList<ParsingDiagnostic> Diagnostics => _diagnostics;

    public UsfmCstParser(ReadOnlyMemory<char> source)
    {
        _source = source;
        _lexer = new UsfmLexer(source.Span);
        _diagnostics = new List<ParsingDiagnostic>();
    }

    public CstRootNode Parse()
    {
        var rootChildren = new List<CstNode>();
        var markerStack = new Stack<(CstMarkerNodeBuilder Builder, List<CstNode> Children)>();

        while (_lexer.TryMoveNext(out var token))
        {
            switch (token.Type)
            {
                case UsfmTokenType.Marker:
                    while (markerStack.Count > 0 && ShouldCloseImplicitScope(markerStack.Peek().Builder.Name.Span, token.Value))
                    {
                        var (implicitBuilder, implicitChildren) = markerStack.Pop();
                        AddNode(implicitBuilder.Build(implicitChildren.ToImmutableArray(), new SourceSpan(token.Offset, 0)), markerStack, rootChildren);
                    }

                    var markerBuilder = new CstMarkerNodeBuilder(token, _source);
                    markerStack.Push((markerBuilder, new List<CstNode>()));
                    break;

                case UsfmTokenType.MarkerEnd:
                    if (markerStack.Count > 0 && markerStack.Peek().Builder.Name.Span.SequenceEqual(token.Value))
                    {
                        var (builder, children) = markerStack.Pop();
                        var markerNode = builder.Build(children.ToImmutableArray(), GetSourceSpan(token));
                        AddNode(markerNode, markerStack, rootChildren);
                    }
                    else
                    {
                        AddNode(new CstTextNode(GetSourceSpan(token), SourceMemory(token)), markerStack, rootChildren);
                        _diagnostics.Add(new ParsingDiagnostic($"Unexpected closing marker: \\{token.Value.ToString()}*", GetSourceSpan(token)));
                    }
                    break;

                case UsfmTokenType.Text:
                    AddNode(new CstTextNode(GetSourceSpan(token), SourceMemory(token)), markerStack, rootChildren);
                    break;

                case UsfmTokenType.AttributePipe:
                    if (markerStack.Count > 0)
                    {
                        ParseAttributes(markerStack.Peek().Builder.Attributes);
                    }
                    else
                    {
                        AddNode(new CstTextNode(GetSourceSpan(token), SourceMemory(token)), markerStack, rootChildren);
                    }
                    break;
                
                case UsfmTokenType.MilestoneStart:
                case UsfmTokenType.MilestoneEnd:
                    var attributes = new List<CstAttributeNode>();
                    ParseAttributes(token, attributes);
                    AddNode(new CstMilestoneNode(GetSourceSpan(token), SourceMemory(token, 1, token.Value.Length), token.Type == UsfmTokenType.MilestoneEnd, [.. attributes]), markerStack, rootChildren);
                    break;
            }
        }

        while (markerStack.Count > 0)
        {
            var (builder, children) = markerStack.Pop();
            var markerNode = builder.Build(children.ToImmutableArray(), new SourceSpan(_source.Length, 0));
            AddNode(markerNode, markerStack, rootChildren);
        }

        return new CstRootNode(new SourceSpan(0, _source.Length), rootChildren.ToImmutableArray());
    }

    private void AddNode(CstNode node, Stack<(CstMarkerNodeBuilder Builder, List<CstNode> Children)> markerStack, List<CstNode> rootChildren)
    {
        if (markerStack.Count > 0)
        {
            markerStack.Peek().Children.Add(node);
        }
        else
        {
            rootChildren.Add(node);
        }
    }

    private void ParseAttributes(List<CstAttributeNode> target)
    {
        while (_lexer.TryPeek(out var nextToken) &&
               nextToken.Type is not (UsfmTokenType.Marker or UsfmTokenType.MarkerEnd or UsfmTokenType.EndOfFile))
        {
            _lexer.TryMoveNext(out var token);

            if (token.Type == UsfmTokenType.Text)
            {
                var text = token.Span;
                int i = 0;
                while (i < text.Length)
                {
                    while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
                    if (i >= text.Length) break;

                    int eqIndex = text[i..].IndexOf('=');
                    if (eqIndex != -1)
                    {
                        var key = text.Slice(i, eqIndex).Trim();
                        i += eqIndex + 1;
                        if (i < text.Length && text[i] == '"')
                        {
                            i++;
                            int nextQuote = text[i..].IndexOf('"');
                            if (nextQuote != -1)
                            {
                                var value = text.Slice(i, nextQuote);
                                target.Add(new CstAttributeNode(new SourceSpan(token.Offset + i - key.Length - 2, key.Length + nextQuote + 3), _source.Slice(token.Offset + i - key.Length - 2, key.Length), _source.Slice(token.Offset + i, value.Length)));
                                i += nextQuote + 1;
                            }
                        }
                    }
                    else
                    {
                        // Shorthand - consume the rest as a single attribute if no = is found
                        var val = text[i..].Trim();
                        target.Add(new CstAttributeNode(new SourceSpan(token.Offset + i, val.Length), "default".AsMemory(), _source.Slice(token.Offset + i, val.Length)));
                        break;
                    }
                }
            }
        }
    }

    private void ParseAttributes(UsfmToken milestone, List<CstAttributeNode> target)
    {
        var span = milestone.Span;
        int pipe = span.IndexOf('|');
        if (pipe < 0)
            return;

        var attributes = span[(pipe + 1)..];
        int end = attributes.LastIndexOf("\\*");
        if (end >= 0)
            attributes = attributes[..end];

        ParseAttributeText(milestone.Offset + pipe + 1, attributes, target);
    }

    private void ParseAttributeText(int offset, ReadOnlySpan<char> text, List<CstAttributeNode> target)
    {
        int i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i >= text.Length) break;
            int equals = text[i..].IndexOf('=');
            if (equals < 0) break;
            var key = text.Slice(i, equals).Trim();
            int valueStart = i + equals + 1;
            while (valueStart < text.Length && char.IsWhiteSpace(text[valueStart])) valueStart++;
            if (valueStart >= text.Length || text[valueStart] != '"') break;
            int valueEnd = text[(valueStart + 1)..].IndexOf('"');
            if (valueEnd < 0) break;
            valueEnd += valueStart + 1;
            target.Add(new CstAttributeNode(new SourceSpan(offset + i, valueEnd - i + 1), _source.Slice(offset + i, key.Length), _source.Slice(offset + valueStart + 1, valueEnd - valueStart - 1)));
            i = valueEnd + 1;
        }
    }

    private SourceSpan GetSourceSpan(UsfmToken token) => new(token.Offset, token.Span.Length);

    private static bool RequiresClosingMarker(ReadOnlySpan<char> marker) =>
        marker.SequenceEqual("w") || marker.SequenceEqual("f") || marker.SequenceEqual("x") ||
        marker.SequenceEqual("add") || marker.SequenceEqual("nd") || marker.SequenceEqual("ord") ||
        marker.SequenceEqual("pn") || marker.SequenceEqual("qt") || marker.SequenceEqual("it");

    private static bool ShouldCloseImplicitScope(ReadOnlySpan<char> current, ReadOnlySpan<char> next)
    {
        if (RequiresClosingMarker(current))
            return false;

        if (current.SequenceEqual("v"))
            return true;

        if (current.SequenceEqual("p") || current.StartsWith('p'))
            return IsBlockMarker(next) && !next.SequenceEqual("v");

        if (current.SequenceEqual("tr"))
            return next.SequenceEqual("tr") || (IsBlockMarker(next) && !IsCellMarker(next));

        if (IsCellMarker(current))
            return IsBlockMarker(next) && !IsCellMarker(next);

        return IsBlockMarker(next);
    }

    private static bool IsCellMarker(ReadOnlySpan<char> marker) =>
        marker.StartsWith("tc") || marker.StartsWith("th");

    private static bool IsBlockMarker(ReadOnlySpan<char> marker) =>
        marker.SequenceEqual("id") || marker.SequenceEqual("c") || marker.SequenceEqual("p") ||
        marker.StartsWith('p') || marker.StartsWith('s') || marker.SequenceEqual("r") ||
        marker.SequenceEqual("m") || marker.StartsWith('h') || marker.StartsWith('i') ||
        marker.StartsWith('l') || marker.StartsWith('t') || marker.StartsWith('q') ||
        marker.StartsWith("mt") || marker.StartsWith("is") || marker.StartsWith("ip") ||
        marker.StartsWith("li") || marker.StartsWith("tr") || marker.StartsWith("th") ||
        marker.StartsWith("tc") || marker.SequenceEqual("cl") || marker.SequenceEqual("cp") ||
        marker.SequenceEqual("cd");

    private ReadOnlyMemory<char> SourceMemory(UsfmToken token, int relativeStart = 0, int? length = null) =>
        _source.Slice(token.Offset + relativeStart, length ?? token.Span.Length - relativeStart);

    private class CstMarkerNodeBuilder
    {
        public int StartOffset { get; }
        public int StartLength { get; }
        public ReadOnlyMemory<char> Name { get; }
        public List<CstAttributeNode> Attributes { get; } = new();

        public CstMarkerNodeBuilder(UsfmToken token, ReadOnlyMemory<char> source)
        {
            StartOffset = token.Offset;
            StartLength = token.Span.Length;
            Name = source.Slice(token.Offset + 1, token.Value.Length);
        }

        public CstMarkerNode Build(ImmutableArray<CstNode> children, SourceSpan endSpan)
        {
            int start = StartOffset;
            int length = endSpan.Start == 0 && endSpan.Length == 0 ? StartLength : endSpan.End - start;
            return new CstMarkerNode(new SourceSpan(start, length), Name, children, [.. Attributes]);
        }
    }
}
