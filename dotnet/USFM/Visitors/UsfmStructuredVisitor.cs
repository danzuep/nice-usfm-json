namespace USFM.Visitors;

public class UsfmStructuredVisitor : BaseStructuredVisitor<IUsfmNode>
{
    protected override IUsfmNode CreateBook(BookNode node) =>
        new BookNode(node.Style, node.Code, node.Description);

    protected override IUsfmNode CreateChapter(ChapterNode node, string sid) =>
        new ChapterNode(node.Style, node.Number);

    protected override IUsfmNode CreateVerse(VerseNode node, string vid) =>
        new VerseNode(node.Style, node.Number);

    protected override IUsfmNode CreatePara(ParaNode node, IList<IUsfmNode>? children) =>
        new ParaNode(node.Style, children);

    protected override IUsfmNode CreateChar(CharNode node, IList<IUsfmNode>? children) =>
        new CharNode(node.Style, children);

    protected override IUsfmNode CreateText(TextNode node) =>
        new TextNode(node.Text);

    protected override IUsfmNode CreateNote(NoteNode node, IList<IUsfmNode>? children) =>
        new NoteNode(node.Style, node.Caller, children);

    // Additional required overrides
    public override void Visit(MilestoneNode node) =>
        AddToResult(new MilestoneNode(node.Style, node.Attributes));
    public override void Visit(LineBreakNode node) =>
        AddToResult(new LineBreakNode(node.Style));

    public override void Visit(TableNode node) =>
        AddToResult(new TableNode(node.Style, ProcessChildren(node.Content)));

    public override void Visit(RowNode node) =>
        AddToResult(new RowNode(node.Style, ProcessChildren(node.Content)));

    public override void Visit(CellNode node) =>
        AddToResult(new CellNode(node.Style, node.Align, ProcessChildren(node.Content)));
}
