using USJ;

namespace USFM.Visitors;

public class UsjConvertingVisitor : IUsfmVisitor
{
    // A stack to track current nesting level (e.g., current children of a Para or Char node)
    private readonly Stack<List<IUsjNode>> _containerStack = new();

    public UsjConvertingVisitor()
    {
        // Initialize with a root list to catch top-level nodes
        _containerStack.Push(new List<IUsjNode>());
    }

    public IReadOnlyList<IUsjNode> GetResult() => _containerStack.Peek().ToArray();

    private void ProcessContainer(IEnumerable<IUsfmNode> children, Action<IList<IUsjNode>> onComplete)
    {
        var localList = new List<IUsjNode>();
        _containerStack.Push(localList);
        this.Accept(children);
        _containerStack.Pop();
        onComplete(localList);
    }

    public void Visit(BookNode node) =>
        ProcessContainer(node.Content, c => _containerStack.Peek()
            .Add(new UsjBook(node.Code, null, c, node.Style)));

    public void Visit(ParaNode node) =>
        ProcessContainer(node.Content, c => _containerStack.Peek()
            .Add(new UsjPara(null, c, node.Style)));

    public void Visit(CharNode node) =>
        ProcessContainer(node.Content, c => _containerStack.Peek()
            .Add(new UsjChar(c, node.Style)));

    public void Visit(NoteNode node) =>
        ProcessContainer(node.Content, c => _containerStack.Peek()
            .Add(new UsjNote(node.Caller, c, node.Style)));

    public void Visit(TableNode node) =>
        ProcessContainer(node.Content, c => _containerStack.Peek()
            .Add(new UsjTable(c, node.Style)));

    public void Visit(RowNode node) =>
        ProcessContainer(node.Content, c => _containerStack.Peek()
            .Add(new UsjRow(c, node.Style)));

    public void Visit(CellNode node) =>
        ProcessContainer(node.Content, c => _containerStack.Peek()
            .Add(new UsjCell(node.Align, c, node.Style)));

    public void Visit(ChapterNode node) =>
        _containerStack.Peek().Add(new UsjChapter(node.Number, null, node.Style));

    public void Visit(VerseNode node) =>
        _containerStack.Peek().Add(new UsjVerse(node.Number, null, node.Style));

    public void Visit(TextNode node) =>
        _containerStack.Peek().Add(new UsjText(node.Text));

    public void Visit(MilestoneNode node) =>
        _containerStack.Peek().Add(new UsjMilestone(node.Sid, node.Eid, null, node.Style));

    public void Visit(LineBreakNode node) =>
        _containerStack.Peek().Add(new UsjLineBreak(node.Style));
}