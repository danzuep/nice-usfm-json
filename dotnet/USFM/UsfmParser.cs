using USFM.Visitors;

namespace USFM;

public class UsfmParser
{
    private static readonly HashSet<string> CharStyles = new()
    {
        "wj", "bd", "it", "bdit", "no", "sc", "add", "bk", "dc", "k", "nd", "ord", "pn", "png", "addpn", "qt", "sig", "sls", "tl", "xt", "va", "vp", "rq", "pro",
        "fr", "ft", "fk", "fq", "fqa", "fl", "fv", "fdc", "fm", "fy", "xo", "xk", "xq", "xta"
    };

    private class InlineContainer
    {
        public string? Type { get; set; } // "char", "note", "cell"
        public string? Style { get; set; }
        public string? Caller { get; set; }
        public string? Align { get; set; }
        public List<IUsfmNode> Content { get; } = new();
    }

    private class ParserState
    {
        public List<IUsfmNode> TopLevelNodes = new();
        public List<IUsfmNode> CurrentBlockContent = new();
        public List<IUsfmNode> TableRowsAccumulator = new();
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

        public void AppendNode(IUsfmNode node)
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
            IUsfmNode finalizedNode = container.Type switch
            {
                "char" => new CharNode(container.Style ?? "", container.Content),
                "note" => new NoteNode(container.Style ?? "f", container.Caller ?? "+", container.Content),
                "cell" => new CellNode(container.Style ?? "cell", container.Align ?? "left", container.Content),
                _ => throw new InvalidOperationException()
            };
            AppendNode(finalizedNode);
        }

        public void FlushTableRows()
        {
            if (TableRowsAccumulator.Count > 0)
            {
                TopLevelNodes.Add(new TableNode("table", new List<IUsfmNode>(TableRowsAccumulator)));
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
                    TableRowsAccumulator.Add(new RowNode(CurrentBlockStyle, new List<IUsfmNode>(CurrentBlockContent)));
                }
                else
                {
                    FlushTableRows();

                    if (CurrentBlockType == "para")
                        TopLevelNodes.Add(new ParaNode(CurrentBlockStyle, new List<IUsfmNode>(CurrentBlockContent)));
                    else if (CurrentBlockType == "book")
                        TopLevelNodes.Add(new BookNode(CurrentBlockStyle, CurrentBlockExtra ?? "", new List<IUsfmNode>(CurrentBlockContent)));
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

    public static IReadOnlyList<IUsfmNode> Parse(ReadOnlySpan<char> usfmData)
    {
        if (usfmData.IsEmpty)
            return Array.Empty<IUsfmNode>();

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