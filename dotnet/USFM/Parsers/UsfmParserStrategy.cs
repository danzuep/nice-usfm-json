using USFM.Lexers;
using USFM.Visitors;

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
        public override string ToString() => Root.Count.ToString();
    }

    public static IReadOnlyList<IUsfmNode> Parse(string usfm)
    {
        var strategy = new UsfmLexerStrategy(usfm.AsSpan());
        var syntaxTree = Parse(ref strategy);
        return syntaxTree;
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
        if (token.Indices.Length == 0)
        {
            state.Add(new TextNode(token.ToString()));
            return;
        }

        var style = UsfmLexerStrategy.GetStyle(token[0]);
        var content = token[1].TrimEnd(' ');

        if (style.IsEmpty)
        {
            state.Add(new TextNode(token.ToString()));
            state.PushInline();
        }
        else if (style.SequenceEqual("v"))
        {
            var segments = UsfmLexerToken.CreateSplit(token).Segments;
            state.Add(new VerseNode(segments[0], segments[1]));
            state.Add(new TextNode(segments[2]));
        }
        else if (style.SequenceEqual("id"))
        {
            var segments = UsfmLexerToken.CreateSplit(token).Segments;
            state.Add(new BookNode(segments[0], segments[1], segments[2]));
        }
        else if (style.SequenceEqual("c"))
        {
            state.Add(new ChapterNode("c", content.ToString()));
        }
        else if (style.EndsWith("-s"))
        {
            ProcessAttributeMilestone(style, content, state);
        }
        else if (style.EndsWith("-e") || style.EndsWith("*"))
        {
            ProcessClosingMarker(style, content, state);
        }
        else if (IdentifyBlock(style) || IdentifyPara(style))
        {
            ProcessBlockMarker(style, content, state);
        }
        else
        {
            ProcessInlineMarker(content, state);
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

    private static bool IdentifyPara(ReadOnlySpan<char> marker)
    {
        return marker.StartsWith("p") || marker.StartsWith("s") || marker.SequenceEqual("r") || marker.SequenceEqual("m");
    }

    private static bool IdentifyBlock(ReadOnlySpan<char> marker)
    {
        return marker.StartsWith("h") || marker.StartsWith("i") || marker.StartsWith("l") ||
            marker.StartsWith("t") || marker.StartsWith("q") || marker.StartsWith("cl") ||
            marker.StartsWith("ca") || marker.StartsWith("cp") || marker.StartsWith("cd") ||
            marker.StartsWith("mt") || marker.StartsWith("is") || marker.StartsWith("ip") ||
            marker.StartsWith("li") || marker.StartsWith("tr") || marker.StartsWith("th") ||
            marker.StartsWith("tc") || marker.SequenceEqual("lh") || marker.SequenceEqual("usfm");
    }

    private static void ParseNode(LexerToken token, ParserState state)
    {
        //switch (style)
        //{
        //    case CharNode w: visitor.Visit(w); break;
        //    case ParaNode p: visitor.Visit(p); break;
        //    case NoteNode n: visitor.Visit(n); break;
        //    case LineBreakNode br: visitor.Visit(br); break;
        //    //case AnnotationNode a: visitor.Visit(a); break;
        //    case BookNode b: visitor.Visit(b); break;
        //    case TableNode t: visitor.Visit(t); break;
        //    case RowNode r: visitor.Visit(r); break;
        //    case CellNode l: visitor.Visit(l); break;
        //}
    }
}