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
    }

    public static IReadOnlyList<IUsfmNode> Parse(ReadOnlySpan<char> usfmData)
    {
        var state = new ParserState();
        var tokenizer = new UsfmTokenizer(usfmData);

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
                    state.PushInline(); // Context for potential nested content
                    if (!token.Value.IsEmpty)
                        state.Add(new TextNode(token.Value.ToString()));
                    break;

                case UsfmMarkerType.Closing:
                    var content = state.PopInline();
                    state.Add(new CharNode(token.Type.TrimEnd('*').ToString(), content));
                    break;

                default: // Raw text tokens
                    if (!token.Value.IsEmpty)
                        state.Add(new TextNode(token.Value.ToString()));
                    break;
            }
        }

        state.ClosePara();
        return state.Root;
    }

    private static void HandleMarker(UsfmToken token, ParserState state)
    {
        switch (token.Type)
        {
            case "id":
                SplitText(token.Value, out var bookSplit);
                var book = new BookNode("id", bookSplit.Type.ToString(), bookSplit.Value.ToString());
                state.Add(book);
                break;
            case "c":
                state.Add(new ChapterNode("c", token.Value.ToString()));
                break;
            case "v":
                SplitText(token.Value, out var verseSplit);
                state.Add(new VerseNode("v", verseSplit.Type.ToString()));
                if (!verseSplit.Value.IsEmpty)
                    state.Add(new TextNode(verseSplit.Value.ToString()));
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
                    state.Add(new TextNode(remainingText.ToString()));
                }
            }
        }
        else
        {
            state.Add(new TextNode(token.Value.ToString()));
        }
    }

    private static void SplitText(ReadOnlySpan<char> input, out UsfmToken token, char splitChar = ' ')
    {
        var nextSpace = input.IndexOf(splitChar);
        if (nextSpace != -1)
        {
            var text = input[..nextSpace];
            var remaining = input[(nextSpace + 1)..];
            token = new UsfmToken(text, remaining);
        }
        else
        {
            token = new UsfmToken(input);
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

        return UsfmMarkerType.Inline;
    }

    private enum UsfmMarkerType { Block, Milestone, Inline, Closing, Text }
}