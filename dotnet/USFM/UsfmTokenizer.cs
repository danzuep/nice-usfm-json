namespace USFM;

public enum UsfmTokenType { Marker, Text }

public readonly ref struct UsfmToken
{
    public UsfmTokenType Type { get; }
    public ReadOnlySpan<char> Value { get; }
    public UsfmToken(UsfmTokenType type, ReadOnlySpan<char> value) { Type = type; Value = value; }
}

public ref struct UsfmTokenizer
{
    private ReadOnlySpan<char> _remaining;
    public UsfmTokenizer(ReadOnlySpan<char> input) => _remaining = input;

    public bool MoveNext(out UsfmToken token)
    {
        if (_remaining.IsEmpty)
        {
            token = default;
            return false;
        }

        if (_remaining[0] == '\\')
        {
            int index = 1;
            while (index < _remaining.Length &&
                   (char.IsLetterOrDigit(_remaining[index]) || _remaining[index] == '*' || _remaining[index] == '-'))
            {
                index++;
            }

            var marker = _remaining.Slice(1, index - 1);
            _remaining = _remaining.Slice(index);

            if (!_remaining.IsEmpty && _remaining[0] == ' ')
                _remaining = _remaining.Slice(1);

            token = new UsfmToken(UsfmTokenType.Marker, marker);
            return true;
        }

        int nextSlash = _remaining.IndexOf('\\');
        if (nextSlash == -1)
        {
            var text = _remaining;
            _remaining = ReadOnlySpan<char>.Empty;
            token = new UsfmToken(UsfmTokenType.Text, text);
            return true;
        }
        else
        {
            var text = _remaining.Slice(0, nextSlash);
            _remaining = _remaining.Slice(nextSlash);
            token = new UsfmToken(UsfmTokenType.Text, text);
            return true;
        }
    }
}

public class UsfmParser
{
    private static readonly HashSet<string> CharStyles = new()
    {
        "wj", "bd", "it", "bdit", "no", "sc", "add", "bk", "dc", "k", "nd", "ord", "pn", "png", "addpn", "qt", "sig", "sls", "tl", "xt", "va", "vp", "rq", "pro",
        "fr", "ft", "fk", "fq", "fqa", "fl", "fv", "fdc", "fm", "fy", "xo", "xk", "xq", "xta"
    };

    private class InlineContainer
    {
        public string Type { get; set; } // "char", "note", "cell"
        public string Style { get; set; }
        public string? Caller { get; set; }
        public string? Align { get; set; }
        public List<UsfmNode> Content { get; } = new();
    }

    private class ParserState
    {
        public List<UsfmNode> TopLevelNodes = new();
        public List<UsfmNode> CurrentBlockContent = new();
        public List<UsfmNode> TableRowsAccumulator = new();
        public Stack<InlineContainer> InlineStack = new();

        public string CurrentBlockType = "para";
        public string CurrentBlockStyle = "p";
        public string? CurrentBlockExtra = null;
        public string? CurrentNoteStyle = null;

        public bool ExpectingChapterNumber = false;
        public bool ExpectingVerseNumber = false;
        public bool ExpectingBookCode = false;
        public bool ExpectingNoteCaller = false;

        public void ClearPendingTextStates()
        {
            if (ExpectingNoteCaller)
            {
                ExpectingNoteCaller = false;
                InlineStack.Push(new InlineContainer { Type = "note", Style = CurrentNoteStyle ?? "f", Caller = "+" });
            }
            ExpectingChapterNumber = false;
            ExpectingVerseNumber = false;
            ExpectingBookCode = false;
        }

        public void AppendNode(UsfmNode node)
        {
            if (InlineStack.Count > 0)
                InlineStack.Peek().Content.Add(node);
            else
                CurrentBlockContent.Add(node);
        }

        public void PopAndAppendInline()
        {
            if (InlineStack.Count == 0) return;
            var container = InlineStack.Pop();
            UsfmNode finalizedNode = container.Type switch
            {
                "char" => new CharNode(container.Style, container.Content),
                "note" => new NoteNode(container.Style, container.Caller ?? "+", container.Content),
                "cell" => new CellNode(container.Style, container.Align ?? "left", container.Content),
                _ => throw new InvalidOperationException()
            };
            AppendNode(finalizedNode);
        }

        public void FlushTableRows()
        {
            if (TableRowsAccumulator.Count > 0)
            {
                TopLevelNodes.Add(new TableNode("table", new List<UsfmNode>(TableRowsAccumulator)));
                TableRowsAccumulator.Clear();
            }
        }

        public void FlushCurrentBlock()
        {
            while (InlineStack.Count > 0) PopAndAppendInline();

            if (CurrentBlockContent.Count > 0 || CurrentBlockStyle != "p")
            {
                if (CurrentBlockType == "row")
                {
                    TableRowsAccumulator.Add(new RowNode(CurrentBlockStyle, new List<UsfmNode>(CurrentBlockContent)));
                }
                else
                {
                    FlushTableRows();

                    if (CurrentBlockType == "para")
                        TopLevelNodes.Add(new ParaNode(CurrentBlockStyle, new List<UsfmNode>(CurrentBlockContent)));
                    else if (CurrentBlockType == "book")
                        TopLevelNodes.Add(new BookNode(CurrentBlockStyle, CurrentBlockExtra ?? "", new List<UsfmNode>(CurrentBlockContent)));
                }
                CurrentBlockContent.Clear();
            }
        }

        public void FinalizeDocument()
        {
            FlushCurrentBlock();
            FlushTableRows();
        }
    }

    public static UsfmNode[] Parse(ReadOnlySpan<char> usfmData)
    {
        var state = new ParserState();
        var tokenizer = new UsfmTokenizer(usfmData);

        while (tokenizer.MoveNext(out var token))
        {
            if (token.Type == UsfmTokenType.Marker)
            {
                string markerStr = token.Value.ToString();
                state.ClearPendingTextStates();

                if (markerStr.EndsWith("*"))
                {
                    state.PopAndAppendInline();
                    continue;
                }

                switch (markerStr)
                {
                    case "id":
                        state.ExpectingBookCode = true;
                        break;
                    case "c":
                        state.ExpectingChapterNumber = true;
                        break;
                    case "v":
                        state.ExpectingVerseNumber = true;
                        break;
                    case "lb":
                        state.AppendNode(new LineBreakNode("lb"));
                        break;
                    case "tr":
                        state.FlushCurrentBlock();
                        state.CurrentBlockType = "row";
                        state.CurrentBlockStyle = "tr";
                        break;
                    case "f":
                    case "fe":
                    case "x":
                    case "ex":
                        state.CurrentNoteStyle = markerStr;
                        state.ExpectingNoteCaller = true;
                        break;
                    default:
                        if (markerStr.StartsWith("th") || markerStr.StartsWith("tc"))
                        {
                            string alignment = markerStr.StartsWith("th") ? "left" : "left";
                            state.InlineStack.Push(new InlineContainer { Type = "cell", Style = markerStr, Align = alignment });
                        }
                        else if (markerStr.StartsWith("ms") || markerStr.Contains("-s") || markerStr.Contains("-e"))
                        {
                            string? sid = markerStr.Contains("-s") ? "start" : null;
                            string? eid = markerStr.Contains("-e") ? "end" : null;
                            state.AppendNode(new MilestoneNode(markerStr, sid, eid));
                        }
                        else if (CharStyles.Contains(markerStr))
                        {
                            state.InlineStack.Push(new InlineContainer { Type = "char", Style = markerStr });
                        }
                        else
                        {
                            state.FlushCurrentBlock();
                            state.CurrentBlockType = "para";
                            state.CurrentBlockStyle = markerStr;
                        }
                        break;
                }
            }
            else if (token.Type == UsfmTokenType.Text)
            {
                string textStr = token.Value.ToString();

                if (state.ExpectingChapterNumber)
                {
                    state.ExpectingChapterNumber = false;
                    state.FlushCurrentBlock();
                    state.TopLevelNodes.Add(new ChapterNode("c", textStr.Trim()));
                }
                else if (state.ExpectingVerseNumber)
                {
                    state.ExpectingVerseNumber = false;
                    state.AppendNode(new VerseNode("v", textStr.Trim()));
                }
                else if (state.ExpectingBookCode)
                {
                    state.ExpectingBookCode = false;
                    state.FlushCurrentBlock();
                    state.CurrentBlockType = "book";
                    state.CurrentBlockStyle = "id";

                    int firstSpace = textStr.IndexOf(' ');
                    if (firstSpace != -1)
                    {
                        state.CurrentBlockExtra = textStr.Substring(0, firstSpace).Trim();
                        string description = textStr.Substring(firstSpace + 1).Trim();
                        if (!string.IsNullOrEmpty(description))
                            state.CurrentBlockContent.Add(new TextNode(description));
                    }
                    else
                    {
                        state.CurrentBlockExtra = textStr.Trim();
                    }
                }
                else if (state.ExpectingNoteCaller)
                {
                    state.ExpectingNoteCaller = false;
                    state.InlineStack.Push(new InlineContainer { Type = "note", Style = state.CurrentNoteStyle ?? "f", Caller = textStr.Trim() });
                }
                else
                {
                    if (!string.IsNullOrEmpty(textStr))
                        state.AppendNode(new TextNode(textStr));
                }
            }
        }

        state.FinalizeDocument();
        return state.TopLevelNodes.ToArray();
    }
}