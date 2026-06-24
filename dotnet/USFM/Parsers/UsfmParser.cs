//using USFM.Lexers;
//using USFM.Visitors;

//namespace USFM.Parsers;

//public class UsfmParser
//{
//    private class ParserState
//    {
//        private readonly List<IUsfmNode> _content = new();

//        public void Add(IUsfmNode node)
//        {
//            _content.Add(node);
//        }

//        public IReadOnlyList<IUsfmNode> Flatten()
//        {
//            return _content;
//        }
//    }

//    public static IReadOnlyList<IUsfmNode> Parse(ReadOnlySpan<char> usfm)
//    {
//        var state = new ParserState();
//        var tokenizer = new UsfmLexerStrategy(usfm);
//        while (tokenizer.TryMoveNext(out var token))
//        {
//            ProcessToken(token, state);
//        }

//        return state.Flatten();
//    }

//    private static void ProcessToken(LexerToken token, ParserState state)
//    {
//        var usfm = new UsfmLexerToken(token);
//        var style = usfm.Style;
//        var content = usfm.Content;

//    }

//    private static void ProcessBlockMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
//    {
//        if (style.StartsWith('p'))
//        {
//            state.OpenPara(style.ToString());
//        }
//        if (!content.IsEmpty)
//        {
//            state.Add(new TextNode(content.ToString()));
//        }
//    }

//    private static void ProcessMilestoneMarker(ReadOnlySpan<char> style, LexerToken token, ParserState state)
//    {
//        if (style.SequenceEqual("id"))
//        {
//            var segments = UsfmLexerToken.SplitValue(token).Segments;
//            state.Add(new BookNode("id", segments[1], segments[2]));
//        }
//        else if (style.SequenceEqual("c"))
//        {
//            state.Add(new ChapterNode("c", token[1].TrimEnd(' ').ToString()));
//        }
//        else if (style.SequenceEqual("v"))
//        {
//            var verse = UsfmLexerToken.SplitValue(token);
//            state.Add(new VerseNode("v", verse.Trimmed(1)));
//            if (!verse[2].IsEmpty)
//            {
//                state.Add(new TextNode(verse[2].ToString()));
//            }
//        }
//        else
//        {
//            ProcessMilestone(style, token[1].TrimEnd(' '), state);
//        }
//    }

//    private static void ProcessInlineMarker(ReadOnlySpan<char> content, ParserState state)
//    {
//        if (!content.IsEmpty)
//        {
//            state.Add(new TextNode(content.ToString()));
//        }
//        state.PushInline();
//    }

//    private static void ProcessClosingMarker(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
//    {
//        var nestedContent = state.PopInline();
//        state.Add(new CharNode(style.ToString(), nestedContent));
//        if (!content.IsEmpty)
//        {
//            state.Add(new TextNode(content.ToString()));
//        }
//    }

//    private static void ProcessAnnotation(LexerToken token, ParserState state)
//    {
//        var segments = new UsfmLexerToken(token).Segments;
//        state.Add(new AnnotationNode(segments[0], segments[1], segments[2]));
//    }

//    private static void ProcessMilestone(ReadOnlySpan<char> style, ReadOnlySpan<char> content, ParserState state)
//    {
//        var attributes = UsfmAttributeParser.Parse(content, out int textStartIndex);
//        state.Add(new MilestoneNode(style.ToString(), attributes));

//        if (textStartIndex != -1 && textStartIndex < content.Length)
//        {
//            var remainingText = content[textStartIndex..];
//            if (!char.IsWhiteSpace(remainingText[0]) && !state.HasInline())
//            {
//                state.Add(new TextNode(" "));
//            }
//            state.Add(new TextNode(remainingText.ToString()));
//        }
//    }

//    public static IUsfmNode CreateNode(UsfmMarkerType type, UsfmLexerTokenDto token)
//    {
//        var node = type switch
//        {
//            UsfmMarkerType.Text => CreateText(token),
//            UsfmMarkerType.Char => CreateChar(token),
//            UsfmMarkerType.Para => CreatePara(token),
//            UsfmMarkerType.Verse => CreateVerse(token),
//            UsfmMarkerType.Chapter => CreateChapter(token),
//            UsfmMarkerType.Note => CreateNote(token),
//            UsfmMarkerType.LineBreak => CreateLineBreak(token),
//            UsfmMarkerType.Milestone => CreateMilestone(token),
//            UsfmMarkerType.Book => CreateBook(token),
//            UsfmMarkerType.Table => CreateTable(token),
//            UsfmMarkerType.Row => CreateRow(token),
//            UsfmMarkerType.Cell => CreateCell(token),
//            _ => throw new NotSupportedException($"Unknown USFM type: {type}")
//        };
//        return node;
//    }

//    public enum UsfmMarkerType { Unknown, Text, Char, Para, Verse, Chapter, Note, LineBreak, Milestone, Book, Table, Row, Cell }
//}