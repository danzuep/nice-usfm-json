using USFM.Lexers;
using USFM.Visitors;

public partial class UsfmParser
{
    private class ParserState
    {
        public readonly List<IUsfmNode> Root = new();
        private readonly Stack<List<IUsfmNode>> _contentStack = new();
        private string? _activeParaStyle;

        public void Add(IUsfmNode node)
        {
            if (_contentStack.Count > 0) _contentStack.Peek().Add(node);
            else Root.Add(node);
        }

        public void OpenPara(string style)
        {
            ClosePara();
            _activeParaStyle = style;
            _contentStack.Push(new List<IUsfmNode>());
        }

        public void ClosePara()
        {
            if (_activeParaStyle == null) return;
            var content = _contentStack.Count > 0 ? _contentStack.Pop() : null;
            Root.Add(new ParaNode(_activeParaStyle, content?.Count > 0 ? content : null));
            _activeParaStyle = null;
        }

        public void PushInline() => _contentStack.Push(new List<IUsfmNode>());
        public IList<IUsfmNode>? PopInline() => _contentStack.Count > 0 ? _contentStack.Pop() : null;
        public bool HasInline() => _contentStack.Count > 0;
    }

    public static IReadOnlyList<IUsfmNode> Parse(ReadOnlySpan<char> usfmData)
    {
        var state = new ParserState();
        var tokenizer = new UsfmLexerStrategy(usfmData);

        while (tokenizer.TryMoveNext(out var token))
        {
            var style = GetStyle(token[0]);
            var type = IdentifyMarker(style, token[2]);

            switch (type)
            {
                case UsfmMarkerType.Block:
                    state.OpenPara(style.ToString());
                    if (!token[1].IsEmpty)
                        state.Add(new TextNode(token[1].ToString()));
                    break;

                case UsfmMarkerType.Milestone:
                    HandleMarker(token, state);
                    break;

                case UsfmMarkerType.Inline:
                    // For inline markers, if there is trailing value text, add it to the current context
                    if (!token[1].IsEmpty)
                        state.Add(new TextNode(token[1].ToString()));
                    state.PushInline(); // Context for potential nested content
                    break;

                case UsfmMarkerType.Closing:
                    var content = state.PopInline();
                    state.Add(new CharNode(style.ToString(), content));
                    if (!token[1].IsEmpty)
                        state.Add(new TextNode(token[1].ToString()));
                    break;

                default: // Raw text tokens
                    if (!token[0].IsEmpty)
                        state.Add(new TextNode(token[0].ToString()));
                    break;
            }
        }

        state.ClosePara();
        //state.Add(new LineBreakNode());
        return state.Root;
    }

    private static void HandleMarker(LexerToken token, ParserState state)
    {
        var style = GetStyle(token[0]);
        switch (style)
        {
            case "id":
                var book = new BookNode("id", token[1].ToString(), token[2].ToString());
                state.Add(book);
                break;
            case "c":
                state.Add(new ChapterNode("c", token[1].ToString()));
                break;
            case "v":
                state.Add(new VerseNode("v", token[1].ToString(), token[2].ToString()));
                break;
            default:
                HandleMilestone(token, state);
                break;
        }
    }

    // Handle attribute-based milestones (like \qt-s, \ts-s, etc.)
    private static void HandleMilestone(LexerToken token, ParserState state)
    {
        if (token[1].IsEmpty)
            return;
        var style = GetStyle(token[0]);
        if (style.EndsWith("-s") || style.EndsWith("-e"))
        {
            var startIndex = token[1][0] == '|' ? 1 : 0;
            var attributes = UsfmAttributeParser.Parse(token[1], out int textStartIndex);
            state.Add(new MilestoneNode(style, attributes));
            // If there is text after the \* delimiter, add it as a TextNode
            if (textStartIndex != -1 && textStartIndex < token[1].Length)
            {
                var remainingText = token[1][textStartIndex..];
                if (!remainingText.IsEmpty)
                {
                    // If the remaining text begins with no whitespace but the milestone is adjacent to prior text,
                    // ensure we add a single space so concatenation matches original line spacing.
                    if (!char.IsWhiteSpace(remainingText[0]) && !state.HasInline())
                    {
                        state.Add(new TextNode(" "));
                    }
                    state.Add(new TextNode(remainingText.ToString()));
                }
            }
        }
        else
        {
            //state.Add(new SeparatorNode(" "));
            state.Add(new TextNode(token[1].ToString()));
        }
    }

    private static UsfmMarkerType IdentifyMarker(ReadOnlySpan<char> marker, ReadOnlySpan<char> extra)
    {
        if (marker.IsEmpty)
            return UsfmMarkerType.Text;
        if (marker is "id" or "c" or "v" || marker.EndsWith("-s") || marker.EndsWith("-e"))
            return UsfmMarkerType.Milestone;
        
        // Check if extra contains a closing marker (e.g., \w* or \*)
        var extraText = extra.Trim();
        if (extraText.IsEmpty || extraText.SequenceEqual("\\*") || marker.SequenceEqual(extraText.TrimStart('\\').TrimEnd('*')))
            return UsfmMarkerType.Closing;

        // Block markers start paragraphs or sections
        if (marker.StartsWith("p") || marker.StartsWith("s") || marker is "r" or "m")
            return UsfmMarkerType.Block;

        // Additional block-like markers commonly used in samples
        if (marker.StartsWith("h") || marker.StartsWith("i") || marker.StartsWith("l") ||
            marker.StartsWith("t") || marker.StartsWith("q") || marker.StartsWith("cl") ||
            marker.StartsWith("ca") || marker.StartsWith("cp") || marker.StartsWith("cd") ||
            marker.StartsWith("mt") || marker.StartsWith("is") || marker.StartsWith("ip") ||
            marker.StartsWith("li") || marker.StartsWith("tr") || marker.StartsWith("th") ||
            marker.StartsWith("tc") || marker == "lh")
            return UsfmMarkerType.Block;
        if (marker == "usfm")
            return UsfmMarkerType.Block;

        return UsfmMarkerType.Inline;
    }

    private enum UsfmMarkerType { Block, Milestone, Inline, Closing, Text }

    private static string GetStyle(ReadOnlySpan<char> rawType)
    {
        return UsfmLexerStrategy.GetStyle(rawType).ToString();
    }
}