using USJ;

namespace USFM;

public class UsjConvertingVisitor : IUsfmVisitor<IUsjNode>
{
    private IList<IUsjNode>? MapContent(IEnumerable<UsfmNode> nodes)
    {
        return nodes.Select(node => node.Accept(this)).ToList();
    }

    public IUsjNode Visit(BookNode node)
    {
        return new UsjIdentification(node.Code, null, MapContent(node.Content), node.Style);
    }

    public IUsjNode Visit(ChapterNode node)
    {
        return new UsjChapter(node.Number, null, node.Style);
    }

    public IUsjNode Visit(VerseNode node)
    {
        return new UsjVerse(node.Number, null, node.Style);
    }

    public IUsjNode Visit(ParaNode node)
    {
        return new UsjPara(null, MapContent(node.Content), node.Style);
    }

    public IUsjNode Visit(CharNode node)
    {
        return new UsjChar(MapContent(node.Content), node.Style);
    }

    public IUsjNode Visit(NoteNode node)
    {
        return new UsjNote(node.Caller, MapContent(node.Content), node.Style);
    }

    public IUsjNode Visit(TableNode node)
    {
        return new UsjTable(MapContent(node.Content), node.Style);
    }

    public IUsjNode Visit(RowNode node)
    {
        return new UsjRow(MapContent(node.Content), node.Style);
    }

    public IUsjNode Visit(CellNode node)
    {
        return new UsjCell(node.Align, MapContent(node.Content), node.Style);
    }

    public IUsjNode Visit(MilestoneNode node)
    {
        return new UsjMilestone(node.Sid, node.Eid, null, node.Style);
    }

    public IUsjNode Visit(LineBreakNode node)
    {
        return new UsjLineBreak(node.Style);
    }

    public IUsjNode Visit(TextNode node)
    {
        return new UsjText(node.Text);
    }
}