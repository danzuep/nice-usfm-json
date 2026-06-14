using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        var syntaxTree = UsfmParser.Parse(rawUsfm.AsSpan());
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

public interface IUsfmNode { }

public sealed class TextNode : IUsfmNode
{
    public string Text { get; }
    public TextNode(string text) => Text = text;
    public override string ToString() => Text;
}

public sealed class BookNode : IUsfmNode
{
    public string Style { get; } = "id";
    public string Code { get; }
    public string? Description { get; }
    public BookNode(string style, string? code, string? description = null)
        { Style = style; Code = code ?? string.Empty; Description = description; }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Description))
            return $"\\{Style} {Code}";
        else
            return $"\\{Style} {Code} {Description}";
    }
}

public sealed class ChapterNode : IUsfmNode
{
    public string Style { get; } = "c";
    public string Number { get; }
    public ChapterNode(string style, string number)
        { Style = style; Number = number; }
    public override string ToString() => $"\\{Style} {Number}";
}

public sealed class VerseNode : IUsfmNode
{
    public string Style { get; } = "v";
    public string Number { get; }
    public string? Text { get; }
    public VerseNode(string style, string number, string? text = null)
        { Style = style; Number = number; Text = text; }
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Text))
            return $"\\{Style} {Number}";
        else
            return $"\\{Style} {Number} {Text}";
    }
}

public sealed class ParaNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public ParaNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (Content != null && Content.Count > 0)
        {
            var first = Content[0]?.ToString() ?? string.Empty;
            if (first.Length > 0 && !char.IsWhiteSpace(first[0])) sb.Append(' ');
            foreach (var child in Content)
                sb.Append(child?.ToString());
        }
        return sb.ToString();
    }
}

public sealed class CharNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public CharNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (Content != null && Content.Count > 0)
        {
            var first = Content[0]?.ToString() ?? string.Empty;
            if (first.Length > 0 && !char.IsWhiteSpace(first[0])) sb.Append(' ');
            foreach (var child in Content)
                sb.Append(child?.ToString());
        }
        sb.AppendFormat("\\{0}*", Style);
        return sb.ToString();
    }
}

public sealed class NoteNode : IUsfmNode
{
    public string Style { get; }
    public string Caller { get; }
    public IList<IUsfmNode>? Content { get; }
    public NoteNode(string style, string caller, IList<IUsfmNode>? content = null)
        { Style = style; Caller = caller; Content = content; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (!string.IsNullOrEmpty(Caller)) sb.Append(' ').Append(Caller);
        if (Content != null && Content.Count > 0)
        {
            var first = Content[0]?.ToString() ?? string.Empty;
            if (first.Length > 0 && !char.IsWhiteSpace(first[0])) sb.Append(' ');
            foreach (var child in Content)
                sb.Append(child?.ToString());
        }
        sb.AppendFormat("\\{0}*", Style);
        return sb.ToString();
    }
}

public sealed class TableNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public TableNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (Content != null && Content.Count > 0)
        {
            var first = Content[0]?.ToString() ?? string.Empty;
            if (first.Length > 0 && !char.IsWhiteSpace(first[0])) sb.Append(' ');
            foreach (var child in Content)
                sb.Append(child?.ToString());
        }
        return sb.ToString();
    }
}

public sealed class RowNode : IUsfmNode
{
    public string Style { get; }
    public IList<IUsfmNode>? Content { get; }
    public RowNode(string style, IList<IUsfmNode>? content = null)
        { Style = style; Content = content; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (Content != null && Content.Count > 0)
        {
            var first = Content[0]?.ToString() ?? string.Empty;
            if (first.Length > 0 && !char.IsWhiteSpace(first[0])) sb.Append(' ');
            foreach (var child in Content)
                sb.Append(child?.ToString());
        }
        return sb.ToString();
    }
}

public sealed class CellNode : IUsfmNode
{
    public string Style { get; }
    public string Align { get; }
    public IList<IUsfmNode>? Content { get; }
    public CellNode(string style, string align, IList<IUsfmNode>? content = null)
        { Style = style; Align = align; Content = content; }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (!string.IsNullOrEmpty(Align)) sb.Append(Align);
        if (Content != null && Content.Count > 0)
        {
            var first = Content[0]?.ToString() ?? string.Empty;
            if (first.Length > 0 && !char.IsWhiteSpace(first[0])) sb.Append(' ');
            foreach (var child in Content)
                sb.Append(child?.ToString());
        }
        return sb.ToString();
    }
}

public sealed class MilestoneNode : IUsfmNode
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

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (Attributes != null && Attributes.Count > 0)
        {
            sb.Append(" |");
            foreach (var kvp in Attributes)
            {
                sb.AppendFormat("{0}=\"{1}\" ", kvp.Key, kvp.Value);
            }
            sb.Append("\\*");
        }
        return sb.ToString();
    }
}

public sealed class AnnotationNode : IUsfmNode
{
    public string Style { get; }
    public string Text { get; }
    public string End { get; }
    public AnnotationNode(string style, string text, string? end = null)
        { Style = style; Text = text; End = end ?? $"\\{Style}*"; }
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Style))
            return $"{Text}{End}";
        else
            return $"\\{Style} {Text}{End}";
    }
}

public sealed class LineBreakNode : IUsfmNode
{
    public string Style { get; }
    public LineBreakNode(string? style = null) =>
        Style = style ?? Environment.NewLine;
    public override string ToString() => Style;
}