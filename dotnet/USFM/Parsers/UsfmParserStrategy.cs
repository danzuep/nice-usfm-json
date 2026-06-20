using USFM.Lexers;
using USFM.Visitors;
using static UsfmParser;

namespace USFM.Parsers;

public class UsfmParserStrategy
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

    public static IReadOnlyList<IUsfmNode> Parse(ref UsfmLexerStrategy tokenizer)
    {
        var state = new ParserState();

        while (tokenizer.TryMoveNext(out var token))
        {
            ProcessToken(token, state);
        }

        state.ClosePara();
        return state.Root;
    }

    private static void ProcessToken(LexerToken token, ParserState state)
    {
        // Raw plain text node fallback
        if (token.Indices.Length == 0)
        {
            state.Add(new TextNode(token.Span.ToString()));
            return;
        }

        var style = token[0].TrimStart('\\').TrimEnd();
        var content = token[1];
        var extra = token.Indices.Length > 1 ? token[2] : ReadOnlySpan<char>.Empty;

        switch (IdentifyMarker(style))
        {
            case UsfmMarkerType.Block:
                ProcessBlockMarker(style, content, state);
                break;
            case UsfmMarkerType.Milestone:
                ProcessMilestoneMarker(style, content, extra, state);
                break;
            case UsfmMarkerType.Inline:
                ProcessInlineMarker(content, state);
                break;
            case UsfmMarkerType.Closing:
                ProcessClosingMarker(style, content, state);
                break;
            default:
                if (!content.IsEmpty) state.Add(new TextNode(content.ToString()));
                break;
        }
    }

    private static void ProcessBlockMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
    {
        state.OpenPara(style.ToString());
        if (!content.IsEmpty)
        {
            state.Add(new TextNode(content.ToString()));
        }
    }

    private static void ProcessMilestoneMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ReadOnlySpan<char> extra, ParserState state)
    {
        if (style.SequenceEqual("id"))
        {
            SplitByFirstSpace(content, out var code, out var title);
            state.Add(new BookNode("id", code.ToString(), title.ToString()));
        }
        else if (style.SequenceEqual("c"))
        {
            state.Add(new ChapterNode("c", content.ToString()));
        }
        else if (style.SequenceEqual("v"))
        {
            SplitByFirstSpace(content, out var number, out var verseText);
            state.Add(new VerseNode("v", number.ToString(), verseText.ToString()));
        }
        else
        {
            ProcessAttributeMilestone(style, content, state);
        }
    }

    private static void ProcessInlineMarker(ReadOnlySpan<char> content, ParserState state)
    {
        if (!content.IsEmpty)
        {
            state.Add(new TextNode(content.ToString()));
        }
        state.PushInline();
    }

    private static void ProcessClosingMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
    {
        var nestedContent = state.PopInline();
        state.Add(new CharNode(style.ToString(), nestedContent));
        if (!content.IsEmpty)
        {
            state.Add(new TextNode(content.ToString()));
        }
    }

    private static void ProcessAttributeMilestone(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
    {
        if (content.IsEmpty) return;

        if (style.EndsWith("-s") || style.EndsWith("-e"))
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
        else
        {
            state.Add(new TextNode(content.ToString()));
        }
    }

    private static void SplitByFirstSpace(ReadOnlySpan<char> span, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
    {
        int spaceIdx = span.IndexOf(' ');
        if (spaceIdx != -1)
        {
            first = span[..spaceIdx];
            second = span[(spaceIdx + 1)..];
        }
        else
        {
            first = span;
            second = ReadOnlySpan<char>.Empty;
        }
    }

    private static UsfmMarkerType IdentifyMarker(ReadOnlySpan<char> marker)
    {
        if (marker.IsEmpty) return UsfmMarkerType.Text;
        if (marker.SequenceEqual("id") || marker.SequenceEqual("c") || marker.SequenceEqual("v") || marker.EndsWith("-s") || marker.EndsWith("-e")) return UsfmMarkerType.Milestone;
        if (marker.EndsWith("*") || marker.StartsWith("qt-e")) return UsfmMarkerType.Closing;
        if (marker.StartsWith("p") || marker.StartsWith("s") || marker.SequenceEqual("r") || marker.SequenceEqual("m")) return UsfmMarkerType.Block;

        if (marker.StartsWith("h") || marker.StartsWith("i") || marker.StartsWith("l") ||
            marker.StartsWith("t") || marker.StartsWith("q") || marker.StartsWith("cl") ||
            marker.StartsWith("ca") || marker.StartsWith("cp") || marker.StartsWith("cd") ||
            marker.StartsWith("mt") || marker.StartsWith("is") || marker.StartsWith("ip") ||
            marker.StartsWith("li") || marker.StartsWith("tr") || marker.StartsWith("th") ||
            marker.StartsWith("tc") || marker.SequenceEqual("lh") || marker.SequenceEqual("usfm"))
            return UsfmMarkerType.Block;

        return UsfmMarkerType.Inline;
    }

    private enum UsfmMarkerType { Block, Milestone, Inline, Closing, Text }
}