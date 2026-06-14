using USFM;
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
        var tokenizer = new UsfmLexer(usfmData);

        while (tokenizer.TryMoveNext(out var token))
        {
            var type = IdentifyMarker(token.Type);

            switch (type)
            {
                case UsfmMarkerType.Block:
                    state.OpenPara(token.Type.ToString());
                    if (!token.Value.IsEmpty)
                        state.Add(new TextNode(token.Value.ToString()));
                    break;

                case UsfmMarkerType.Milestone:
                    HandleMarker(token, state);
                    break;

                case UsfmMarkerType.Inline:
                    // For inline markers, if there is trailing value text, add it to the current context
                    if (!token.Value.IsEmpty)
                        state.Add(new TextNode(token.Value.ToString()));
                    state.PushInline(); // Context for potential nested content
                    break;

                case UsfmMarkerType.Closing:
                    var content = state.PopInline();
                    state.Add(new CharNode(token.Type.ToString().TrimEnd('*'), content));
                    if (!token.Value.IsEmpty)
                        state.Add(new TextNode(token.Value.ToString()));
                    break;

                default: // Raw text tokens
                    if (!token.Value.IsEmpty)
                        state.Add(new TextNode(token.Value.ToString()));
                    break;
            }
        }

        state.ClosePara();
        //state.Add(new LineBreakNode());
        return state.Root;
    }

    private static void HandleMarker(UsfmToken token, ParserState state)
    {
        switch (token.Type)
        {
            case "id":
                var book = new BookNode("id", token.Value.ToString(), token.Extra.ToString());
                state.Add(book);
                break;
            case "c":
                state.Add(new ChapterNode("c", token.Value.ToString()));
                break;
            case "v":
                state.Add(new VerseNode("v", token.Value.ToString(), token.Extra.ToString()));
                break;
            default:
                HandleMilestone(token, state);
                break;
        }
    }

    // Handle attribute-based milestones (like \qt-s, \ts-s, etc.)
    private static void HandleMilestone(UsfmToken token, ParserState state)
    {
        if (token.Value.IsEmpty)
            return;
        if (token.Type.EndsWith("-s") || token.Type.EndsWith("-e"))
        {
            var startIndex = token.Value[0] == '|' ? 1 : 0;
            var attributes = UsfmAttributeParser.Parse(token.Value, out int textStartIndex);
            state.Add(new MilestoneNode(token.Type.ToString(), attributes));
            // If there is text after the \* delimiter, add it as a TextNode
            if (textStartIndex != -1 && textStartIndex < token.Value.Length)
            {
                var remainingText = token.Value[textStartIndex..];
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
            state.Add(new TextNode(token.Value.ToString()));
        }
    }

    private static UsfmMarkerType IdentifyMarker(ReadOnlySpan<char> marker)
    {
        if (marker.IsEmpty)
            return UsfmMarkerType.Text;
        if (marker is "id" or "c" or "v" || marker.EndsWith("-s") || marker.EndsWith("-e"))
            return UsfmMarkerType.Milestone;
        if (marker.EndsWith("*") || marker.StartsWith("qt-e"))
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
}