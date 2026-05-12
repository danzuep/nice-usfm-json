using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;

namespace USFM.Visitors;

public interface IUsfmVisitor
{
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
        var syntaxTree = UsfmParser.Parse(rawUsfm.AsSpan());
        visitor.Accept(syntaxTree);
    }

    public static async Task ParseAsync(this IUsfmVisitor visitor, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            visitor.Accept(line);
        }
    }
}

public interface IUsfmNode { }

public class TextNode : IUsfmNode
{
    public string Text { get; }
    public TextNode(string text) => Text = text;
    public override string ToString() => Text;
}

public class BookNode : IUsfmNode
{
    public string Style { get; } = "id";
    public string Code { get; }
    public string? Description { get; }
    public BookNode(string style, string? code, string? description = null)
        { Style = style; Code = code ?? string.Empty; Description = description; }
    public override string ToString() => $"{Style} {Code} {Description}";
}

public class ChapterNode : IUsfmNode
{
    public string Style { get; } = "c";
    public string Number { get; }
    public ChapterNode(string style, string number)
        { Style = style; Number = number; }
    public override string ToString() => $"{Style} {Number}";
}

public class VerseNode : IUsfmNode
{
    public string Style { get; } = "v";
    public string Number { get; }
    public VerseNode(string style, string number)
        { Style = style; Number = number; }
    public override string ToString() => $"{Style} {Number}";
}

public class ParaNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public ParaNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString() => $"{Style} {Content?.Count ?? 0}";
}

public class CharNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public CharNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString() => $"{Style} {Content?.Count ?? 0}";
}

public class NoteNode : IUsfmNode
{
    public string Style { get; }
    public string Caller { get; }
    public IList<IUsfmNode>? Content { get; }
    public NoteNode(string style, string caller, IList<IUsfmNode>? content = null)
        { Style = style; Caller = caller; Content = content; }
    public override string ToString() => $"{Style} {Caller} {Content?.Count ?? 0}";
}

public class TableNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public TableNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString() => $"{Style} {Content?.Count ?? 0}";
}

public class RowNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public RowNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString() => $"{Style} {Content?.Count ?? 0}";
}

public class CellNode : IUsfmNode
{
    public string Style { get; }
    public string Align { get; }
    public IList<IUsfmNode>? Content { get; }
    public CellNode(string style, string align, IList<IUsfmNode>? content = null)
        { Style = style; Align = align; Content = content; }
    public override string ToString() => $"{Style} {Align} {Content?.Count ?? 0}";
}

public class MilestoneNode : IUsfmNode
{
    public string Style { get; }
    public string? StartId => Attributes.GetValueOrDefault("sid");
    public string? EndId => Attributes.GetValueOrDefault("eid");
    public string? Who => Attributes.GetValueOrDefault("who");
    public string? Level => Attributes.GetValueOrDefault("level");
    public IReadOnlyDictionary<string, string> Attributes { get; }

    public MilestoneNode(string style, IReadOnlyDictionary<string, string> attributes)
    {
        Style = style;
        Attributes = attributes;
    }

    public override string ToString() => $"{Style} {StartId} {EndId} {Who} {Level}";
}

public class LineBreakNode : IUsfmNode
{
    public string Style { get; }
    public LineBreakNode(string style) => Style = style;
    public override string ToString() => Style;
}