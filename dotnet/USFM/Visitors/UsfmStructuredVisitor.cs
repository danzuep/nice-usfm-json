namespace USFM.Visitors;

public class UsfmStructuredVisitor : BaseStructuredVisitor<IUsfmNode>
{
    protected override IUsfmNode CreateBook(BookNode node) =>
        new BookNode(node.Style, node.Code, node.Description);

    protected override IUsfmNode CreateChapter(ChapterNode node, string sid) =>
        new ChapterNode(node.Style, node.Number);

    protected override IUsfmNode CreateVerse(VerseNode node, string vid) =>
        new VerseNode(node.Style, node.Number, node.Text);

    protected override IUsfmNode CreatePara(ParaNode node, string? vid, IList<IUsfmNode>? children) =>
        new ParaNode(node.Style, children);

    protected override IUsfmNode CreateChar(CharNode node, IList<IUsfmNode>? children) =>
        new CharNode(node.Style, children, node.Attributes);

    protected override IUsfmNode CreateText(TextNode node) =>
        new TextNode(node.Text);

    protected override IUsfmNode CreateNote(NoteNode node, IList<IUsfmNode>? children) =>
        new NoteNode(node.Style, node.Caller, children);

    protected override IUsfmNode CreateMilestone(MilestoneNode node) =>
        new MilestoneNode(node.Style, node.Attributes);

    protected override IUsfmNode CreateLineBreak(LineBreakNode node) =>
        new LineBreakNode(node.Style);

    protected override IUsfmNode CreateTable(TableNode node, IList<IUsfmNode>? children) =>
        new TableNode(node.Style, children);

    protected override IUsfmNode CreateRow(RowNode node, IList<IUsfmNode>? children) =>
        new RowNode(node.Style, children);

    protected override IUsfmNode CreateCell(CellNode node, IList<IUsfmNode>? children) =>
        new CellNode(node.Style, node.Align, children);

    protected override IUsfmNode CreateAnnotation(AnnotationNode node) =>
        new AnnotationNode(node.Style, node.Text, node.End);
}
