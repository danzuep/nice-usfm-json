using USFM.Lexers;
using USFM.Parsers;

namespace USFM.Visitors;

public interface IUsfmVisitor
{
    //void Visit(AnnotationNode node);
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

public static class UsfmVisitorExtensions
{
    public static void Accept(this IUsfmVisitor visitor, IUsfmNode? usjNode)
    {
        if (usjNode == null) return;
        switch (usjNode)
        {
            case TextNode s: visitor.Visit(s); break;
            case CharNode w: visitor.Visit(w); break;
            case ParaNode p: visitor.Visit(p); break;
            case VerseNode v: visitor.Visit(v); break;
            case ChapterNode c: visitor.Visit(c); break;
            case NoteNode n: visitor.Visit(n); break;
            case LineBreakNode br: visitor.Visit(br); break;
            case MilestoneNode ms: visitor.Visit(ms); break;
            //case AnnotationNode a: visitor.Visit(a); break;
            case BookNode b: visitor.Visit(b); break;
            case TableNode t: visitor.Visit(t); break;
            case RowNode r: visitor.Visit(r); break;
            case CellNode l: visitor.Visit(l); break;
            default:
                throw new NotSupportedException($"Unknown USFM type: {usjNode.GetType()}");
        }
    }

    public static void Accept(this IUsfmVisitor visitor, IEnumerable<IUsfmNode>? content)
    {
        if (content == null) return;
        foreach (var item in content)
            visitor.Accept(item);
    }

    public static void Accept(this IUsfmVisitor visitor, string rawUsfm)
    {
        if (string.IsNullOrEmpty(rawUsfm)) return;
        var strategy = new UsfmLexerStrategy(rawUsfm.AsSpan());
        var syntaxTree = UsfmParserStrategy.Parse(ref strategy);
        visitor.Accept(syntaxTree);
    }

    public static async Task ParseAsync(this IUsfmVisitor visitor, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        await visitor.ParseAsync(reader, cancellationToken);
    }

    public static async Task ParseAsync(this IUsfmVisitor visitor, StreamReader reader, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(reader);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            visitor.Accept(line);
        }
    }
}
