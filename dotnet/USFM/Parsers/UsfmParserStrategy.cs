using USFM.Lexers;
using USFM.Visitors;
using static UsfmParser;

namespace USFM.Parsers;

public class UsfmParserStrategy : IParserStrategy<UsfmLexerStrategy, IReadOnlyList<IUsfmNode>>
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

    public IReadOnlyList<IUsfmNode> Parse(ref UsfmLexerStrategy tokenizer)
    {
        var state = new ParserState();

        while (tokenizer.TryMoveNext(out var token))
        {
            // Extract component views from the unified LexerToken span using your split indices
            var style = GetStyle(token);
            var value = GetValue(token);
            var extra = GetExtra(token);

            var type = IdentifyMarker(style);

            switch (type)
            {
                case UsfmMarkerType.Block:
                    state.OpenPara(style.ToString());
                    if (!value.IsEmpty)
                        state.Add(new TextNode(value.ToString()));
                    break;

                case UsfmMarkerType.Milestone:
                    HandleMarker(style, value, extra, state);
                    break;

                case UsfmMarkerType.Inline:
                    if (!value.IsEmpty)
                        state.Add(new TextNode(value.ToString()));
                    state.PushInline();
                    break;

                case UsfmMarkerType.Closing:
                    var content = state.PopInline();
                    state.Add(new CharNode(style.ToString(), content));
                    if (!value.IsEmpty)
                        state.Add(new TextNode(value.ToString()));
                    break;

                default: // Raw text tokens
                    if (!value.IsEmpty)
                        state.Add(new TextNode(value.ToString()));
                    break;
            }
        }

        state.ClosePara();
        return state.Root;
    }

    // Helper partition slicers mapping LexerToken back into original component expectations
    private static ReadOnlySpan<char> GetStyle(in LexerToken token)
    {
        if (token.Indexes.Count == 0) return ReadOnlySpan<char>.Empty;
        var rawStyle = token.Span[..token.Indexes[0]];
        if (!rawStyle.IsEmpty && rawStyle[0] == '\\') rawStyle = rawStyle[1..];
        return rawStyle.Trim();
    }

    private static ReadOnlySpan<char> GetValue(in LexerToken token)
    {
        if (token.Indexes.Count == 0) return token.Span;
        if (token.Indexes.Count == 1) return token.Span[token.Indexes[0]..];
        return token.Span[token.Indexes[0]..token.Indexes[1]];
    }

    private static ReadOnlySpan<char> GetExtra(in LexerToken token)
    {
        if (token.Indexes.Count <= 1) return ReadOnlySpan<char>.Empty;
        return token.Span[token.Indexes[1]..];
    }

    private static void HandleMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> value, ReadOnlySpan<char> extra, ParserState state)
    {
        if (style.SequenceEqual("id"))
        {
            state.Add(new BookNode("id", value.ToString(), extra.ToString()));
        }
        else if (style.SequenceEqual("c"))
        {
            state.Add(new ChapterNode("c", value.ToString()));
        }
        else if (style.SequenceEqual("v"))
        {
            state.Add(new VerseNode("v", value.ToString(), extra.ToString()));
        }
        else
        {
            HandleMilestone(style, value, state);
        }
    }

    private static void HandleMilestone(ReadOnlySpan<char> style, ReadOnlySpan<char> value, ParserState state)
    {
        if (value.IsEmpty)
            return;

        if (style.EndsWith("-s") || style.EndsWith("-e"))
        {
            var attributes = UsfmAttributeParser.Parse(value, out int textStartIndex);
            state.Add(new MilestoneNode(style.ToString(), attributes));

            if (textStartIndex != -1 && textStartIndex < value.Length)
            {
                var remainingText = value[textStartIndex..];
                if (!remainingText.IsEmpty)
                {
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
            state.Add(new TextNode(value.ToString()));
        }
    }

    private static UsfmMarkerType IdentifyMarker(ReadOnlySpan<char> marker)
    {
        if (marker.IsEmpty)
            return UsfmMarkerType.Text;

        if (marker.SequenceEqual("id") || marker.SequenceEqual("c") || marker.SequenceEqual("v") || marker.EndsWith("-s") || marker.EndsWith("-e"))
            return UsfmMarkerType.Milestone;

        if (marker.EndsWith("*") || marker.StartsWith("qt-e"))
            return UsfmMarkerType.Closing;

        if (marker.StartsWith("p") || marker.StartsWith("s") || marker.SequenceEqual("r") || marker.SequenceEqual("m"))
            return UsfmMarkerType.Block;

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