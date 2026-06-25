using USFM.Lexers;
using USFM.Visitors;

namespace USFM.Parsers;

public class UsfmParser
{
    public static IReadOnlyList<IUsfmNode> Parse(ReadOnlySpan<char> usfm)
    {
        var nodes = new List<IUsfmNode>();
        var tokenizer = new UsfmLexerStrategy(usfm);

        while (tokenizer.TryMoveNext(out var token))
        {
            ProcessToken(token, nodes);
        }

        return nodes;
    }

    private static void ProcessToken(LexerToken lexerToken, List<IUsfmNode> nodes)
    {
        var token = new UsfmLexerToken(lexerToken);
        var type = GetUsfmMarkerType(token.Style);

        // Re-wrap with a split value token if it's a marker requiring segmented parts (e.g., id, v)
        if (type == UsfmMarkerType.Book || type == UsfmMarkerType.Verse)
        {
            token = new UsfmLexerToken(lexerToken).SplitValue();
        }

        var node = CreateNode(type, token);
        nodes.Add(node);

        // If a verse token contains trailing inline text in its remaining segments, emit it flatly
        if (type == UsfmMarkerType.Verse && lexerToken.Indices.Length > 1 && !lexerToken[2].IsEmpty)
        {
            nodes.Add(new TextNode(lexerToken[2].ToString()));
        }
    }

    private static UsfmMarkerType GetUsfmMarkerType(ReadOnlySpan<char> style)
    {
        if (style.IsEmpty) return UsfmMarkerType.Text;
        if (style.SequenceEqual("id")) return UsfmMarkerType.Book;
        if (style.SequenceEqual("c")) return UsfmMarkerType.Chapter;
        if (style.SequenceEqual("v")) return UsfmMarkerType.Verse;
        if (style.StartsWith('f') || style.StartsWith('x')) return UsfmMarkerType.Note;
        if (style.StartsWith('p')) return UsfmMarkerType.Para;
        if (style.StartsWith("lb")) return UsfmMarkerType.LineBreak;
        if (style.StartsWith('w')) return UsfmMarkerType.Char;
        if (style.StartsWith("tr")) return UsfmMarkerType.Row;
        if (style.StartsWith("tc") || style.StartsWith("th")) return UsfmMarkerType.Cell;
        if (style.StartsWith("t")) return UsfmMarkerType.Table;

        return UsfmMarkerType.Milestone;
    }

    internal static IUsfmNode CreateNode(UsfmMarkerType type, UsfmLexerToken token)
    {
        return type switch
        {
            UsfmMarkerType.Text => CreateText(token),
            UsfmMarkerType.Para => CreatePara(token),
            UsfmMarkerType.Char => CreateChar(token),
            UsfmMarkerType.Verse => CreateVerse(token),
            UsfmMarkerType.Chapter => CreateChapter(token),
            UsfmMarkerType.Note => CreateNote(token),
            UsfmMarkerType.LineBreak => CreateLineBreak(token),
            UsfmMarkerType.Milestone => CreateMilestone(token),
            UsfmMarkerType.Book => CreateBook(token),
            UsfmMarkerType.Table => CreateTable(token),
            UsfmMarkerType.Row => CreateRow(token),
            UsfmMarkerType.Cell => CreateCell(token),
            _ => throw new NotSupportedException($"Unknown USFM type: {type}")
        };
    }

    private static IUsfmNode CreateText(UsfmLexerToken token)
    {
        return new TextNode(token.ToString());
    }

    private static IUsfmNode CreatePara(UsfmLexerToken token)
    {
        return new ParaNode(token.Style.ToString());
    }
    private static IUsfmNode CreateVerse(UsfmLexerToken token)
    {
        return new VerseNode("v", token.Content.ToString(), token.Extra.ToString());
    }

    private static IUsfmNode CreateChapter(UsfmLexerToken token)
    {
        return new ChapterNode("c", token.Content.ToString());
    }

    private static IUsfmNode CreateMilestone(UsfmLexerToken token)
    {
        var attributes = UsfmAttributeParser.Parse(token.ToString(), out _);
        return new MilestoneNode(token.Style.ToString(), attributes);
    }

    private static IUsfmNode CreateBook(UsfmLexerToken token)
    {
        return new BookNode("id", token.Content.ToString(), token.Extra.ToString());
    }

    private static IUsfmNode CreateChar(UsfmLexerToken token)
    {
        var nestedContent = new List<IUsfmNode>();
        return new CharNode(token.Style.ToString(), nestedContent);
    }

    private static IUsfmNode CreateNote(UsfmLexerToken token)
    {
        return new NoteNode(token.Style.ToString(), token.Content.ToString());
    }

    private static IUsfmNode CreateLineBreak(UsfmLexerToken token)
    {
        return new LineBreakNode(token.Style.ToString());
    }

    private static IUsfmNode CreateTable(UsfmLexerToken token)
    {
        return new TableNode(token.Style.ToString());
    }

    private static IUsfmNode CreateRow(UsfmLexerToken token)
    {
        return new RowNode(token.Style.ToString());
    }

    private static IUsfmNode CreateCell(UsfmLexerToken token)
    {
        return new CellNode(token.Style.ToString(), token.Content.ToString());
    }

    public enum UsfmMarkerType { Unknown, Text, Char, Para, Verse, Chapter, Note, LineBreak, Milestone, Book, Table, Row, Cell }
}