using System.Text;
using USFM.Parsers;

namespace USFM.Ast;

public interface IUsfmNode { }

public readonly record struct UsfmAttribute(string Key, string Value, SourceSpan Span);

public sealed class UsfmAttributeCollection : IReadOnlyList<UsfmAttribute>
{
    private readonly UsfmAttribute[] _items;

    public UsfmAttributeCollection(IEnumerable<UsfmAttribute>? attributes = null) =>
        _items = attributes?.ToArray() ?? [];

    public int Count => _items.Length;
    public UsfmAttribute this[int index] => _items[index];
    public string? this[string key] => GetValueOrDefault(key);

    public string? GetValueOrDefault(string key) =>
        _items.FirstOrDefault(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal)).Value;

    public IEnumerator<UsfmAttribute> GetEnumerator() => ((IEnumerable<UsfmAttribute>)_items).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

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
    public string StartVerse { get; }
    public string? EndVerse { get; }
    public string? Text { get; }
    public VerseNode(string style, string number, string? text = null)
    {
        Style = style;
        Number = number;
        Text = text;

        var rangeSeparator = number.AsSpan().IndexOf('-');
        if (rangeSeparator < 0)
        {
            StartVerse = number;
            return;
        }

        StartVerse = number[..rangeSeparator];
        EndVerse = number[(rangeSeparator + 1)..];
    }
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
    public UsfmAttributeCollection Attributes { get; }
    public IList<IUsfmNode>? Content { get; }
    public CharNode(string style, IList<IUsfmNode>? content = null, UsfmAttributeCollection? attributes = null)
        { Style = style; Content = content; Attributes = attributes ?? new UsfmAttributeCollection(); }
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("\\{0}", Style);
        if (Attributes.Count > 0)
        {
            sb.Append(" |");
            foreach (var attribute in Attributes)
                sb.Append($"{attribute.Key}=\"{attribute.Value}\" ");
        }
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
    public UsfmAttributeCollection Attributes { get; }

    public MilestoneNode(string style, UsfmAttributeCollection attributes)
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