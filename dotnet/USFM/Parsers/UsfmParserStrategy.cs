using System.Reflection.Metadata;
using USFM.Lexers;
using USFM.Visitors;

namespace USFM.Parsers;

public class UsfmParserStrategy
{
    private class ParserState
    {
        public readonly List<IUsfmNode> Root = new();
        private readonly Stack<List<IUsfmNode>> _contentStack = new();
        private readonly Stack<string> _inlineStyles = new();
        private List<IUsfmNode>? _tableContent;
        private List<IUsfmNode>? _rowContent;
        private List<IUsfmNode>? _cellContent;
        private string? _cellStyle;
        private string? _activeParaStyle;

        public void Add(IUsfmNode node)
        {
            if (_cellContent != null) _cellContent.Add(node);
            else if (_contentStack.Count > 0) _contentStack.Peek().Add(node);
            else Root.Add(node);
        }

        public void OpenPara(string style)
        {
            CloseTable();
            ClosePara();
            _activeParaStyle = style;
            _contentStack.Push(new List<IUsfmNode>());
        }

        public void ClosePara()
        {
            if (_activeParaStyle == null) return;
            CloseInlineFrames();
            var content = _contentStack.Count > 0 ? _contentStack.Pop() : null;
            if (content?.LastOrDefault() is TextNode text)
                content[^1] = new TextNode(text.Text.TrimEnd(' ', '\r', '\n'));
            Root.Add(new ParaNode(_activeParaStyle, content?.Count > 0 ? content : null));
            _activeParaStyle = null;
        }

        public void CloseRoot()
        {
            CloseInlineFrames();
            ClosePara();
            CloseTable();

            if (_contentStack.Count > 0)
            {
                var content = _contentStack.Pop();
                Root.AddRange(content);
            }
        }

        public void PushInline(string style)
        {
            _contentStack.Push(new List<IUsfmNode>());
            _inlineStyles.Push(style);
        }

        public IUsfmNode? PopInline()
        {
            if (_inlineStyles.Count == 0 || _contentStack.Count == 0) return null;

            var content = _contentStack.Pop();
            var openingStyle = _inlineStyles.Pop();
            return new CharNode(openingStyle, content);
        }

        public void CloseInlineFrames()
        {
            while (_inlineStyles.Count > 0)
            {
                CloseInline();
            }
        }

        public void CloseInline()
        {
            var node = PopInline();
            if (node != null) Add(node);
        }

        public bool HasInline() => _inlineStyles.Count > 0;

        public string? CurrentInlineStyle => _inlineStyles.Count > 0 ? _inlineStyles.Peek() : null;

        public void OpenTable()
        {
            ClosePara();
            _tableContent ??= new List<IUsfmNode>();
        }

        public void OpenRow()
        {
            CloseCell();
            CloseRow();
            OpenTable();
            _rowContent = new List<IUsfmNode>();
        }

        public void OpenCell(string style)
        {
            CloseCell();
            _cellStyle = style;
            _cellContent = new List<IUsfmNode>();
        }

        public void CloseCell()
        {
            if (_cellContent == null || _cellStyle == null || _rowContent == null) return;
            var align = _cellStyle.StartsWith("tcr") ? "end" : "start";
            _rowContent.Add(new CellNode(_cellStyle, align, _cellContent));
            _cellContent = null;
            _cellStyle = null;
        }

        public void CloseRow()
        {
            CloseCell();
            if (_rowContent == null || _tableContent == null) return;
            _tableContent.Add(new RowNode("tr", _rowContent));
            _rowContent = null;
        }

        public void CloseTable()
        {
            CloseRow();
            if (_tableContent == null) return;
            Root.Add(new TableNode(string.Empty, _tableContent));
            _tableContent = null;
        }
    }

    public static IReadOnlyList<IUsfmNode> Parse(ReadOnlySpan<char> usfm)
    {
        var strategy = new UsfmLexerStrategy(usfm);
        return Parse(ref strategy);
    }

    public static IReadOnlyList<IUsfmNode> Parse(ref UsfmLexerStrategy tokenizer)
    {
        var state = new ParserState();

        while (tokenizer.TryMoveNext(out var token))
        {
            ProcessToken(token, state);
        }

        state.CloseRoot();
        return state.Root;
    }

    private static void ProcessToken(LexerToken token, ParserState state)
    {
        // Raw plain text node fallback
        if (token.Indices.Length == 0)
        {
            var text = token.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                state.Add(new TextNode(text));
            return;
        }

        if (token.Span.TrimEnd(' ').EndsWith('*'))
        {
            ProcessAnnotation(token, state);
            return;
        }

        var style = UsfmLexerStrategy.GetStyle(token[0]);
        var content = token[1];
        if (!content.IsEmpty && content[0] != '\\')
            content = content.TrimEnd();

        switch (IdentifyMarker(style))
        {
            case UsfmMarkerType.Block:
                ProcessBlockMarker(style, content, state);
                break;
            case UsfmMarkerType.Milestone:
                ProcessMilestoneMarker(style, token, state);
                break;
            case UsfmMarkerType.Inline:
                ProcessInlineMarker(style, content, state);
                break;
            case UsfmMarkerType.Closing:
                ProcessClosingMarker(style, token[1], state);
                break;
            case UsfmMarkerType.Text:
                state.Add(new TextNode(token.ToString()));
                break;
        }
    }

    private static void ProcessBlockMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
    {
        if (style.SequenceEqual("tr"))
        {
            state.OpenRow();
        }
        else if (style.StartsWith("th") || style.StartsWith("tc"))
        {
            state.OpenCell(style.ToString());
        }
        else
        {
            state.OpenPara(style.ToString());
        }
        if (!content.IsEmpty)
        {
            state.Add(new TextNode(content.ToString().TrimEnd('\r', '\n')));
        }
    }

    private static bool IsTableMarker(ReadOnlySpan<char> style) =>
        style.SequenceEqual("tr") ||
        style.StartsWith("th") ||
        style.StartsWith("tc");

    private static void ProcessMilestoneMarker(ReadOnlySpan<char> style, LexerToken token, ParserState state)
    {
        if (style.SequenceEqual("id"))
        {
            var segments = new UsfmLexerToken(token).SplitValue().Segments;
            state.Add(new BookNode("id", segments[1], string.IsNullOrEmpty(segments[2]) ? null : segments[2]));
        }
        else if (style.SequenceEqual("c"))
        {
            state.ClosePara();
            state.Add(new ChapterNode("c", token[1].TrimEnd().ToString()));
        }
        else if (style.SequenceEqual("v"))
        {
            var verse = new UsfmLexerToken(token).SplitValue();
            state.Add(new VerseNode("v", verse.Trimmed(1)));
            if (!verse[2].IsEmpty)
            {
                var verseText = verse[2].ToString().TrimEnd('\r', '\n');
                if (verseText.Length > 0 && verse[2].EndsWith("\r\n".AsSpan()))
                    verseText += " ";
                state.Add(new TextNode(verseText));
            }
        }
        else
        {
            ProcessMilestone(style, token[1].TrimEnd(), state);
        }
    }

    private static void ProcessInlineMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
    {
        if ((style.SequenceEqual("xo") || style.SequenceEqual("xt")) && state.HasInline())
            state.CloseInline();

        state.PushInline(style.ToString());
        if (!content.IsEmpty)
        {
            state.Add(new TextNode(content.ToString().TrimEnd('\r', '\n')));
        }
    }

    private static void ProcessClosingMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
    {
        var inlineNode = state.PopInline();
        if (inlineNode != null)
            state.Add(inlineNode);
        if (!content.IsEmpty)
        {
            state.Add(new TextNode(content.ToString()));
        }
    }

    private static void ProcessAnnotation(LexerToken token, ParserState state)
    {
        var segments = new UsfmLexerToken(token).Segments;
        var style = segments[0];
        var content = segments[1];

        if (style is "f" or "x")
        {
            var callerEnd = content.IndexOf(' ');
            var caller = callerEnd < 0 ? content : content[..callerEnd];
            var nestedRaw = callerEnd < 0 ? string.Empty : content[(callerEnd + 1)..];
            var nested = nestedRaw.Length == 0 ? null : Parse(nestedRaw).ToList();
            if (nested != null)
            {
                for (var index = 0; index < nested.Count; index++)
                {
                    if (nested[index] is CharNode { Style: "xo" or "xt" } character)
                    {
                        nested[index] = new CharNode(character.Style, character.Content,
                            new Dictionary<string, string> { ["closed"] = "false" });
                    }
                }
            }
            state.Add(new NoteNode(style, caller, nested));
            return;
        }

        var pipe = content.IndexOf('|');
        var text = pipe < 0 ? content : content[..pipe];
        var children = text.Length == 0 ? new List<IUsfmNode>() : Parse(text.ToString()).ToList();
        IReadOnlyDictionary<string, string> attributes = new Dictionary<string, string>();
        if (style.SequenceEqual("ca"))
            attributes = new Dictionary<string, string> { ["status"] = "invalid" };
        if (pipe >= 0)
        {
            attributes = UsfmAttributeParser.Parse(content, out _);
            if (attributes.Count == 0)
            {
                var shorthand = content[(pipe + 1)..].Trim();
                if (shorthand.Length > 0)
                    attributes = new Dictionary<string, string> { ["lemma"] = shorthand.ToString() };
            }
        }

        state.Add(new CharNode(style, children, attributes));
    }

    private static void ProcessMilestone(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
    {
        var attributes = UsfmAttributeParser.Parse(content, out int textStartIndex);
        state.Add(new MilestoneNode(style.ToString(), attributes));

        if (textStartIndex != -1 && textStartIndex < content.Length)
        {
            var remainingText = content[textStartIndex..];
            if (!char.IsWhiteSpace(remainingText[0]) && !state.HasInline())
            {
                state.Add(new TextNode(" "));
            }
            state.Add(new TextNode(remainingText.ToString()));
        }
    }

    private static UsfmMarkerType IdentifyMarker(ReadOnlySpan<char> marker)
    {
        if (marker.IsEmpty) return UsfmMarkerType.Text;

        if (marker.SequenceEqual("id") || marker.SequenceEqual("c") || marker.SequenceEqual("v") || marker.EndsWith("-s") || marker.EndsWith("-e"))
            return UsfmMarkerType.Milestone;

        if (marker.StartsWith("w"))
            return UsfmMarkerType.Attribute;

        if (marker.StartsWith("f") || marker.StartsWith("x"))
            return UsfmMarkerType.Inline;

        if (marker.EndsWith("*") || marker.StartsWith("qt-e"))
            return UsfmMarkerType.Closing;

        if (marker.StartsWith("p") || marker.StartsWith("s") || marker.SequenceEqual("r") || marker.SequenceEqual("m") ||
            marker.StartsWith("h") || marker.StartsWith("i") || marker.StartsWith("l") ||
            marker.StartsWith("t") || marker.StartsWith("q") || marker.StartsWith("cl") ||
            marker.SequenceEqual("cp") || marker.SequenceEqual("cd") ||
            marker.StartsWith("mt") || marker.StartsWith("is") || marker.StartsWith("ip") ||
            marker.StartsWith("li") || marker.StartsWith("tr") || marker.StartsWith("th") ||
            marker.StartsWith("tc") || marker.SequenceEqual("lh") || marker.SequenceEqual("usfm"))
            return UsfmMarkerType.Block;

        return UsfmMarkerType.Inline;
    }

    private enum UsfmMarkerType { Block, Milestone, Inline, Closing, Text, Attribute }
}