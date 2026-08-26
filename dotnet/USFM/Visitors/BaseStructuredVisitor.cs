using USFM.Parsers;

namespace USFM.Visitors;

public abstract class BaseStructuredVisitor<TNode> : IUsfmVisitor
{
    protected readonly Stack<List<TNode>> ContainerStack = new();
    protected readonly ParsingContext Context = new();

    protected BaseStructuredVisitor()
    {
        ContainerStack.Push(new List<TNode>());
    }

    protected IList<TNode>? ProcessChildren(IEnumerable<IUsfmNode>? children)
    {
        if (children == null || !children.Any()) return null;

        var localList = new List<TNode>();
        ContainerStack.Push(localList);
        this.Accept(children);
        return ContainerStack.Pop();
    }

    protected void AddToResult(TNode node) => ContainerStack.Peek().Add(node);

    public virtual void Visit(BookNode node)
    {
        Context.Book = node.Code;
        Context.Chapter = string.Empty;
        Context.Verse = string.Empty;
        AddToResult(CreateBook(node));
    }

    public virtual void Visit(ChapterNode node)
    {
        Context.Chapter = node.Number;
        Context.Verse = string.Empty;
        AddToResult(CreateChapter(node, Context.ToString()));
    }

    public virtual void Visit(VerseNode node)
    {
        Context.Verse = node.Number;
        AddToResult(CreateVerse(node, Context.ToString()));
    }

    public virtual void Visit(ParaNode node) => AddToResult(CreatePara(node, ProcessChildren(node.Content)));
    public virtual void Visit(CharNode node) => AddToResult(CreateChar(node, ProcessChildren(node.Content)));
    public virtual void Visit(TextNode node) => AddToResult(CreateText(node));
    public virtual void Visit(NoteNode node) => AddToResult(CreateNote(node, ProcessChildren(node.Content)));

    // MOVED FROM LOWER-LEVEL BOILERPLATE TO CORE STRUCTURAL PIPELINE
    public virtual void Visit(MilestoneNode node) => AddToResult(CreateMilestone(node));
    public virtual void Visit(LineBreakNode node) => AddToResult(CreateLineBreak(node));
    public virtual void Visit(TableNode node) => AddToResult(CreateTable(node, ProcessChildren(node.Content)));
    public virtual void Visit(RowNode node) => AddToResult(CreateRow(node, ProcessChildren(node.Content)));
    public virtual void Visit(CellNode node) => AddToResult(CreateCell(node, ProcessChildren(node.Content)));
    public virtual void Visit(AnnotationNode node) => AddToResult(CreateAnnotation(node));

    // New uniform factory lifecycle methods
    protected abstract TNode CreateBook(BookNode node);
    protected abstract TNode CreateChapter(ChapterNode node, string sid);
    protected abstract TNode CreateVerse(VerseNode node, string vid);
    protected abstract TNode CreatePara(ParaNode node, IList<TNode>? children);
    protected abstract TNode CreateChar(CharNode node, IList<TNode>? children);
    protected abstract TNode CreateText(TextNode node);
    protected abstract TNode CreateNote(NoteNode node, IList<TNode>? children);
    protected abstract TNode CreateMilestone(MilestoneNode node);
    protected abstract TNode CreateLineBreak(LineBreakNode node);
    protected abstract TNode CreateTable(TableNode node, IList<TNode>? children);
    protected abstract TNode CreateRow(RowNode node, IList<TNode>? children);
    protected abstract TNode CreateCell(CellNode node, IList<TNode>? children);
    protected abstract TNode CreateAnnotation(AnnotationNode node);

    public IReadOnlyList<TNode> GetResult() => ContainerStack.Peek().ToArray();

    public IReadOnlyList<TNode> FinalizeResult()
    {
        var result = GetResult();
        ContainerStack.Clear();
        Context.Reset();
        return result;
    }
}