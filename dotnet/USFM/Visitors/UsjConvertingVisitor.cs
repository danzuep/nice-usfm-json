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

    // Additional required overrides
    public override void Visit(MilestoneNode node) =>
        AddToResult(new UsjMilestone(node.StartId, node.EndId, node.Who, node.Style));

    public override void Visit(LineBreakNode node) =>
        AddToResult(new UsjLineBreak(node.Style));

    public override void Visit(TableNode node) =>
        AddToResult(new UsjTable(ProcessChildren(node.Content), node.Style));

    public override void Visit(RowNode node) =>
        AddToResult(new UsjRow(ProcessChildren(node.Content), node.Style));

    public override void Visit(CellNode node) =>
        AddToResult(new UsjCell(node.Align, ProcessChildren(node.Content), node.Style));
}
