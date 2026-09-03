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
                        ParseAttributes(markerStack.Peek().Children);
                    }
                    else
                    {
                        AddNode(new CstTextNode(GetSourceSpan(token), SourceMemory(token)), markerStack, rootChildren);
                    }
                    break;
                
                case UsfmTokenType.MilestoneStart:
                case UsfmTokenType.MilestoneEnd:
                    AddNode(new CstMilestoneNode(GetSourceSpan(token), SourceMemory(token, 1, token.Value.Length), token.Type == UsfmTokenType.MilestoneEnd, ImmutableArray<CstNode>.Empty), markerStack, rootChildren);
                    break;
            }
        }

        while (markerStack.Count > 0)
        {
            var (builder, children) = markerStack.Pop();
            var markerNode = builder.Build(children.ToImmutableArray(), SourceSpan.Empty);
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

    private void ParseAttributes(List<CstNode> target)
    {
        while (_lexer.TryMoveNext(out var token))
        {
            if (token.Type is UsfmTokenType.Marker or UsfmTokenType.MarkerEnd or UsfmTokenType.EndOfFile)
            {
                // In a real parser, we'd need to backtrack here. 
                // Since our lexer is forward-only, we'd need to peek.
                // For now, let's assume attributes are well-formed and followed by a marker or end of line.
                break;
            }

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

    private SourceSpan GetSourceSpan(UsfmToken token) => new(token.Offset, token.Span.Length);

    private ReadOnlyMemory<char> SourceMemory(UsfmToken token, int relativeStart = 0, int? length = null) =>
        _source.Slice(token.Offset + relativeStart, length ?? token.Span.Length - relativeStart);

    private class CstMarkerNodeBuilder
    {
        public int StartOffset { get; }
        public int StartLength { get; }
        public ReadOnlyMemory<char> Name { get; }

        public CstMarkerNodeBuilder(UsfmToken token, ReadOnlyMemory<char> source)
        {
            StartOffset = token.Offset;
            StartLength = token.Span.Length;
            Name = source.Slice(token.Offset + 1, token.Value.Length);
        }

        public CstMarkerNode Build(ImmutableArray<CstNode> children, SourceSpan endSpan)
        {
            int start = StartOffset;
            int length = endSpan.Length == 0 ? StartLength : endSpan.End - start;
            return new CstMarkerNode(new SourceSpan(start, length), Name, children);
        }
    }
}
