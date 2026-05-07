namespace USFM;

public interface IUsfmVisitor<out TR>
{
    TR Visit(BookNode node);
    TR Visit(ChapterNode node);
    TR Visit(VerseNode node);
    TR Visit(ParaNode node);
    TR Visit(CharNode node);
    TR Visit(NoteNode node);
    TR Visit(TableNode node);
    TR Visit(RowNode node);
    TR Visit(CellNode node);
    TR Visit(MilestoneNode node);
    TR Visit(LineBreakNode node);
    TR Visit(TextNode node);
}

public abstract class UsfmNode
{
    public abstract TR Accept<TR>(IUsfmVisitor<TR> visitor);
}

public class TextNode : UsfmNode
{
    public string Text { get; }
    public TextNode(string text) => Text = text;
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class BookNode : UsfmNode
{
    public string Style { get; }
    public string Code { get; }
    public List<UsfmNode> Content { get; }
    public BookNode(string style, string code, List<UsfmNode> content)
    { Style = style; Code = code; Content = content; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class ChapterNode : UsfmNode
{
    public string Style { get; }
    public string Number { get; }
    public ChapterNode(string style, string number) { Style = style; Number = number; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class VerseNode : UsfmNode
{
    public string Style { get; }
    public string Number { get; }
    public VerseNode(string style, string number) { Style = style; Number = number; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class ParaNode : UsfmNode
{
    public string Style { get; }
    public List<UsfmNode> Content { get; }
    public ParaNode(string style, List<UsfmNode> content) { Style = style; Content = content; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class CharNode : UsfmNode
{
    public string Style { get; }
    public List<UsfmNode> Content { get; }
    public CharNode(string style, List<UsfmNode> content) { Style = style; Content = content; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class NoteNode : UsfmNode
{
    public string Style { get; }
    public string Caller { get; }
    public List<UsfmNode> Content { get; }
    public NoteNode(string style, string caller, List<UsfmNode> content)
    { Style = style; Caller = caller; Content = content; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class TableNode : UsfmNode
{
    public string Style { get; }
    public List<UsfmNode> Content { get; }
    public TableNode(string style, List<UsfmNode> content) { Style = style; Content = content; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class RowNode : UsfmNode
{
    public string Style { get; }
    public List<UsfmNode> Content { get; }
    public RowNode(string style, List<UsfmNode> content) { Style = style; Content = content; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class CellNode : UsfmNode
{
    public string Style { get; }
    public string Align { get; }
    public List<UsfmNode> Content { get; }
    public CellNode(string style, string align, List<UsfmNode> content)
    { Style = style; Align = align; Content = content; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class MilestoneNode : UsfmNode
{
    public string Style { get; }
    public string? Sid { get; }
    public string? Eid { get; }
    public MilestoneNode(string style, string? sid, string? eid) { Style = style; Sid = sid; Eid = eid; }
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}

public class LineBreakNode : UsfmNode
{
    public string Style { get; }
    public LineBreakNode(string style) => Style = style;
    public override TR Accept<TR>(IUsfmVisitor<TR> visitor) => visitor.Visit(this);
}