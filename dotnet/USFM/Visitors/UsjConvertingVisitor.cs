using USJ;

namespace USFM.Visitors;

public class UsjConvertingVisitor : BaseStructuredVisitor<IUsjNode>
{
    protected override IUsjNode CreateBook(BookNode node) =>
        new UsjBook(node.Code, node.Description, null, node.Style);

    protected override IUsjNode CreateChapter(ChapterNode node, string startId) =>
        new UsjChapter(node.Number, startId, node.Style);

    protected override IUsjNode CreateVerse(VerseNode node, string startId) =>
        new UsjVerse(node.Number, startId, node.Style);

    protected override IUsjNode CreatePara(ParaNode node, IList<IUsjNode>? children) =>
        new UsjPara(null, children, node.Style);

    protected override IUsjNode CreateChar(CharNode node, IList<IUsjNode>? children) =>
        new UsjChar(children, node.Style);

    protected override IUsjNode CreateText(TextNode node) =>
        new UsjText(node.Text);

    protected override IUsjNode CreateNote(NoteNode node, IList<IUsjNode>? children) =>
        new UsjNote(node.Caller, children, node.Style);

    protected override IUsjNode CreateMilestone(MilestoneNode node) =>
        new UsjMilestone(node.StartId, node.EndId, node.Who, node.Style);

    protected override IUsjNode CreateLineBreak(LineBreakNode node) =>
        new UsjLineBreak(node.Style);

    protected override IUsjNode CreateTable(TableNode node, IList<IUsjNode>? children) =>
        new UsjTable(ProcessChildren(node.Content), node.Style);

    protected override IUsjNode CreateRow(RowNode node, IList<IUsjNode>? children) =>
        new UsjRow(ProcessChildren(node.Content), node.Style);

    protected override IUsjNode CreateCell(CellNode node, IList<IUsjNode>? children) =>
        new UsjCell(node.Align, ProcessChildren(node.Content), node.Style);
}
