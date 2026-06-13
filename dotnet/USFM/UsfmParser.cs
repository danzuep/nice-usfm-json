using USFM;
using USFM.Visitors;

public partial class UsfmParser
{
    private class ParserState
    {
        public readonly List<IUsfmNode> Root = new();
        private readonly Stack<List<IUsfmNode>> _contentStack = new();
        // Parallel stack to track the marker name associated with each content frame.
        // For paragraph frames this will be null; for inline frames it contains the marker name
        // (e.g. "x", "xo"). This allows matching closing markers to the correct frame.
        private readonly Stack<string?> _markerStack = new();
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
            _markerStack.Push(null);
        }

        public void ClosePara()
        {
            if (_activeParaStyle == null) return;
            var content = _contentStack.Count > 0 ? _contentStack.Pop() : null;
            if (_markerStack.Count > 0) _markerStack.Pop();
            Root.Add(new ParaNode(_activeParaStyle, content?.Count > 0 ? content : null));
            _activeParaStyle = null;
        }

        public void PushInline(string marker)
        {
            _contentStack.Push(new List<IUsfmNode>());
            _markerStack.Push(marker);
        }

        // Pop the nearest inline frame matching the provided marker name. If not found,
        // pop the top-most inline frame.
        public IList<IUsfmNode>? PopInline(string marker)
        {
            if (_markerStack.Count == 0 || _contentStack.Count == 0)
                return null;

            // If top matches, pop and return
            if (_markerStack.Peek() == marker)
            {
                _markerStack.Pop();
                return _contentStack.Pop();
            }

            // Otherwise, search down the stack for the matching marker
            var tempMarkers = new List<string?>();
            var tempContents = new List<List<IUsfmNode>>();
            while (_markerStack.Count > 0 && _markerStack.Peek() != marker)
            {
                tempMarkers.Add(_markerStack.Pop());
                tempContents.Add(_contentStack.Pop());
            }
            // If we found a match, pop it
            IList<IUsfmNode>? result = null;
            if (_markerStack.Count > 0 && _markerStack.Peek() == marker)
            {
                _markerStack.Pop();
                result = _contentStack.Pop();
            }
            // push back any frames we popped that did not match (preserve order)
            for (int i = tempMarkers.Count - 1; i >= 0; i--)
            {
                _markerStack.Push(tempMarkers[i]);
                _contentStack.Push(tempContents[i]);
            }
            return result;
        }

        public bool HasInline() => _markerStack.Any(m => m != null);
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
                    var nodes = HandleMarker(token);
                    foreach (var node in nodes) state.Add(node);
                    break;

                case UsfmMarkerType.Inline:
                    // For inline markers, push an inline context with the marker name so the matching
                    // closing marker can pop the correct frame. Then add any trailing text as content.
                    state.PushInline(token.Type.ToString()); // Context for potential nested content
                    if (!token.Value.IsEmpty)
                        state.Add(new TextNode(token.Value.ToString()));
                    break;

                case UsfmMarkerType.Closing:
                    var content = state.PopInline(token.Type.ToString().TrimEnd('*'));
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
        return state.Root;
    }

    private static IReadOnlyList<IUsfmNode> HandleMarker(UsfmToken token)
    {
        var nodes = new List<IUsfmNode>();
        switch (token.Type)
        {
            case "id":
                SplitText(token.Value, out var bookSplit);
                var book = new BookNode("id", bookSplit.Type.ToString(), bookSplit.Value.ToString());
                nodes.Add(book);
                break;
            case "c":
                nodes.Add(new ChapterNode("c", token.Value.ToString()));
                break;
            case "v":
                SplitText(token.Value, out var verseSplit);
                nodes.Add(new VerseNode("v", verseSplit.Type.ToString()));
                if (!verseSplit.Value.IsEmpty)
                {
                    nodes.Add(new LineBreakNode(" "));
                    nodes.Add(new TextNode(verseSplit.Value.ToString()));
                }
                else if (!token.Value.IsEmpty && token.Value[token.Value.Length - 1] == ' ')
                {
                    // Preserve a trailing space after a verse marker when the next token is an inline marker
                    nodes.Add(new LineBreakNode(" "));
                }
                break;
            default:
                HandleMilestone(token, nodes);
                break;
        }
        return nodes;
    }

    // Handle attribute-based milestones (like \qt-s, \ts-s, etc.)
    private static void HandleMilestone(UsfmToken token, List<IUsfmNode> nodes)
    {
        if (token.Value.IsEmpty)
            return;
        if (token.Type.EndsWith("-s") || token.Type.EndsWith("-e"))
        {
            var startIndex = token.Value[0] == '|' ? 1 : 0;
            var attributes = UsfmAttributeParser.Parse(token.Value, out int textStartIndex);
            nodes.Add(new MilestoneNode(token.Type.ToString(), attributes));
            // If there is text after the \* delimiter, add it as a TextNode
            if (textStartIndex != -1 && textStartIndex < token.Value.Length)
            {
                var remainingText = token.Value[textStartIndex..];
                if (!remainingText.IsEmpty)
                {
                    nodes.Add(new TextNode(remainingText.ToString()));
                }
            }
        }
        else
        {
            //state.Add(new SeparatorNode(" "));
            nodes.Add(new TextNode(token.Value.ToString()));
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