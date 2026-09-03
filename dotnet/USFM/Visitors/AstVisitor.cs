namespace USFM.Ast;

public interface IUsfmAstVisitor
{
    void Visit(AnnotationNode node);
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

public static class AstVisitorExtensions
{
    public static void Accept(this IUsfmAstVisitor visitor, IUsfmNode? node)
    {
        if (node == null) return;
        switch (node)
        {
            case TextNode text: visitor.Visit(text); break;
            case CharNode character: visitor.Visit(character); break;
            case ParaNode paragraph: visitor.Visit(paragraph); break;
            case VerseNode verse: visitor.Visit(verse); break;
            case ChapterNode chapter: visitor.Visit(chapter); break;
            case NoteNode note: visitor.Visit(note); break;
            case LineBreakNode lineBreak: visitor.Visit(lineBreak); break;
            case MilestoneNode milestone: visitor.Visit(milestone); break;
            case AnnotationNode annotation: visitor.Visit(annotation); break;
            case BookNode book: visitor.Visit(book); break;
            case TableNode table: visitor.Visit(table); break;
            case RowNode row: visitor.Visit(row); break;
            case CellNode cell: visitor.Visit(cell); break;
            default: throw new NotSupportedException($"Unknown USFM AST node: {node.GetType()}");
        }
    }

    public static void Accept(this IUsfmAstVisitor visitor, IEnumerable<IUsfmNode>? nodes)
    {
        if (nodes == null) return;
        foreach (var node in nodes)
            visitor.Accept(node);
    }
}