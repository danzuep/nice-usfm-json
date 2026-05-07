namespace USFM.Visitors;

public interface IUsfmVisitor
{
    void Visit(BookNode node);
    void Visit(ChapterNode node);
    void Visit(VerseNode node);
    void Visit(ParaNode node);
    void Visit(CharNode node);
    void Visit(NoteNode node);
    void Visit(TableNode node);
    void Visit(RowNode node);
    void Visit(CellNode node);
    void Visit(MilestoneNode node);
    void Visit(LineBreakNode node);
    void Visit(TextNode node);
}

public static class UsfmVisitorExtensions
{
    public static void Accept(this IUsfmVisitor visitor, IUsfmNode? usjNode)
    {
        if (usjNode == null) return;
        switch (usjNode)
        {
            case TextNode s: visitor.Visit(s); break;
            case CharNode w: visitor.Visit(w); break;
            case ParaNode p: visitor.Visit(p); break;
            case VerseNode v: visitor.Visit(v); break;
            case ChapterNode c: visitor.Visit(c); break;
            case NoteNode n: visitor.Visit(n); break;
            case LineBreakNode br: visitor.Visit(br); break;
            case MilestoneNode ms: visitor.Visit(ms); break;
            case BookNode b: visitor.Visit(b); break;
            case TableNode t: visitor.Visit(t); break;
            case RowNode r: visitor.Visit(r); break;
            case CellNode l: visitor.Visit(l); break;
            default:
                throw new NotSupportedException($"Unknown USFM type: {usjNode.GetType()}");
        }
    }

    public static void Accept(this IUsfmVisitor visitor, IEnumerable<IUsfmNode>? content)
    {
        if (content == null) return;
        foreach (var item in content)
            visitor.Accept(item);
    }

    public static void Accept(this IUsfmVisitor visitor, string rawUsfm)
    {
        if (string.IsNullOrEmpty(rawUsfm)) return;
        var syntaxTree = UsfmParser.Parse(rawUsfm.AsSpan());
        visitor.Accept(syntaxTree);
    }
}

public interface IUsfmNode { }

public class TextNode : IUsfmNode
{
    public string Text { get; }
    public TextNode(string text) => Text = text;
}

public class BookNode : IUsfmNode
{
    public string Style { get; }
    public string Code { get; }
    public List<IUsfmNode> Content { get; }
    public BookNode(string style, string code, List<IUsfmNode> content)
        { Style = style; Code = code; Content = content; }
}

public class ChapterNode : IUsfmNode
{
    public string Style { get; }
    public string Number { get; }
    public ChapterNode(string style, string number)
        { Style = style; Number = number; }
}

public class VerseNode : IUsfmNode
{
    public string Style { get; }
    public string Number { get; }
    public VerseNode(string style, string number)
        { Style = style; Number = number; }
}

public class ParaNode : IUsfmNode
{
    public string Style { get; }
    public List<IUsfmNode> Content { get; }
    public ParaNode(string style, List<IUsfmNode> content)
        { Style = style; Content = content; }
}

public class CharNode : IUsfmNode
{
    public string Style { get; }
    public List<IUsfmNode> Content { get; }
    public CharNode(string style, List<IUsfmNode> content)
        { Style = style; Content = content; }
}

public class NoteNode : IUsfmNode
{
    public string Style { get; }
    public string Caller { get; }
    public List<IUsfmNode> Content { get; }
    public NoteNode(string style, string caller, List<IUsfmNode> content)
        { Style = style; Caller = caller; Content = content; }
}

public class TableNode : IUsfmNode
{
    public string Style { get; }
    public List<IUsfmNode> Content { get; }
    public TableNode(string style, List<IUsfmNode> content)
        { Style = style; Content = content; }
}

public class RowNode : IUsfmNode
{
    public string Style { get; }
    public List<IUsfmNode> Content { get; }
    public RowNode(string style, List<IUsfmNode> content)
        { Style = style; Content = content; }
}

public class CellNode : IUsfmNode
{
    public string Style { get; }
    public string Align { get; }
    public List<IUsfmNode> Content { get; }
    public CellNode(string style, string align, List<IUsfmNode> content)
        { Style = style; Align = align; Content = content; }
}

public class MilestoneNode : IUsfmNode
{
    public string Style { get; }
    public string? Sid { get; }
    public string? Eid { get; }
    public MilestoneNode(string style, string? sid, string? eid)
        { Style = style; Sid = sid; Eid = eid; }
}

public class LineBreakNode : IUsfmNode
{
    public string Style { get; }
    public LineBreakNode(string style) => Style = style;
}