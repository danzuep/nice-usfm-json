namespace USFM.Visitors;

public abstract class BaseStructuredVisitor<TNode> : IUsfmVisitor
{
    protected readonly Stack<List<TNode>> ContainerStack = new();
    protected readonly ParsingContext Context = new();

    protected BaseStructuredVisitor()
    {
        ContainerStack.Push(new List<TNode>());
    }

    // Helper to process nested content and return a list of nodes
    protected IList<TNode>? ProcessChildren(IEnumerable<IUsfmNode>? children)
    {
        if (children == null || !children.Any()) return null;

        var localList = new List<TNode>();
        ContainerStack.Push(localList);
        this.Accept(children);
        return ContainerStack.Pop();
    }

    // Standard method to add a node to the current active container
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

    public virtual void Visit(ParaNode node) =>
        AddToResult(CreatePara(node, ProcessChildren(node.Content)));

    public virtual void Visit(CharNode node) =>
        AddToResult(CreateChar(node, ProcessChildren(node.Content)));

    public virtual void Visit(TextNode node) =>
        AddToResult(CreateText(node));

    public virtual void Visit(NoteNode node) =>
        AddToResult(CreateNote(node, ProcessChildren(node.Content)));

    // Factory methods for concrete implementations
    protected abstract TNode CreateBook(BookNode node);
    protected abstract TNode CreateChapter(ChapterNode node, string sid);
    protected abstract TNode CreateVerse(VerseNode node, string vid);
    protected abstract TNode CreatePara(ParaNode node, IList<TNode>? children);
    protected abstract TNode CreateChar(CharNode node, IList<TNode>? children);
    protected abstract TNode CreateText(TextNode node);
    protected abstract TNode CreateNote(NoteNode node, IList<TNode>? children);

    // Common visitor boilerplate
    public abstract void Visit(TableNode node);
    public abstract void Visit(RowNode node);
    public abstract void Visit(CellNode node);
    public abstract void Visit(MilestoneNode node);
    public abstract void Visit(LineBreakNode node);

    public IReadOnlyList<TNode> GetResult() =>
        ContainerStack.Peek().ToArray();

    public IReadOnlyList<TNode> FinalizeResult()
    {
        var result = GetResult();
        ContainerStack.Clear();
        Context.Reset();
        return result;
    }
}