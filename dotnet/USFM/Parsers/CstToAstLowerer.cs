using System.Collections.Immutable;
using USFM.Ast;

namespace USFM.Parsers;

public static class CstToAstLowerer
{
    public static IReadOnlyList<IUsfmNode> Lower(CstRootNode root, ReadOnlyMemory<char> source)
    {
        ArgumentNullException.ThrowIfNull(root);
        var result = new List<IUsfmNode>(root.Children.Length);
        foreach (var node in root.Children)
            LowerNode(node, source, result);
        return GroupInlineContinuations(GroupTables(result));
    }

    public static IReadOnlyList<IUsfmNode> Parse(ReadOnlyMemory<char> source, out IReadOnlyList<ParsingDiagnostic> diagnostics)
    {
        var parser = new UsfmCstParser(source);
        var root = parser.Parse();
        diagnostics = parser.Diagnostics;
        return Lower(root, source);
    }

    private static void LowerNode(CstNode node, ReadOnlyMemory<char> source, List<IUsfmNode> result)
    {
        switch (node)
        {
            case CstTextNode text:
                var normalized = NormalizeText(text.Text.Span);
                if (normalized != null)
                    result.Add(new TextNode(normalized));
                break;
            case CstMilestoneNode milestone:
                result.Add(new MilestoneNode(milestone.MarkerName.ToString(), Attributes(milestone.Attributes)));
                break;
            case CstMarkerNode marker:
                LowerMarker(marker, source, result);
                break;
        }
    }

    private static void LowerMarker(CstMarkerNode marker, ReadOnlyMemory<char> source, List<IUsfmNode> result)
    {
        var style = marker.MarkerName.ToString().TrimStart('+');
        var children = LowerChildren(marker.Children, source);
        var text = FlattenText(marker.Children);
        var attributes = Attributes(marker.Attributes).ToList();
        var shorthandIndex = attributes.FindIndex(attribute => attribute.Key == "default");
        if (style == "w" && shorthandIndex >= 0)
            attributes[shorthandIndex] = attributes[shorthandIndex] with { Key = "lemma" };
        if (style == "ca" && attributes.Count == 0)
            attributes.Add(new UsfmAttribute("status", "invalid", SourceSpan.Empty));
        var attributeCollection = new UsfmAttributeCollection(attributes);

        switch (style)
        {
            case "id":
                var description = NormalizeMarkerText(RemainingAfterFirstWord(text));
                result.Add(new BookNode(style, FirstWord(text), string.IsNullOrEmpty(description) ? null : description));
                break;
            case "c":
                result.Add(new ChapterNode(style, FirstWord(text)));
                break;
            case "v":
                var verseText = NormalizeVerseText(RemainingAfterFirstWord(text), marker.Span, source);
                result.Add(new VerseNode(style, FirstWord(text), string.IsNullOrEmpty(verseText) ? null : verseText));
                LowerVerseContent(marker.Children, source, result);
                break;
            case "w":
            case "add":
            case "ca":
            case "nd":
            case "ord":
            case "pn":
            case "ior":
            case "bk":
            case "jmp":
            case "va":
            case "vp":
            case "xo":
            case "xt":
            case "fr":
            case "ft":
            case "fq":
            case "fqa":
            case "fv":
            case "xk":
            case "xq":
                result.Add(new CharNode(style, IsNoteSubmarkerStyle(style) ? TrimTrailingText(children) : children, attributeCollection));
                break;
            case "f":
            case "x":
                result.Add(new NoteNode(style, FirstWord(text), RemoveNoteCaller(children, FirstWord(text))));
                break;
            case var _ when style.StartsWith('p'):
                result.Add(new ParaNode(style, children));
                break;
            case "cl":
            case "cp":
            case "cd":
            case "toc1":
            case "toc2":
            case "toc3":
            case "toca1":
            case "toca2":
            case "toca3":
            case "usfm":
            case "ms":
            case "mr":
            case var _ when style.StartsWith('s'):
            case "r":
            case "m":
            case var _ when style.StartsWith('h'):
            case var _ when style.StartsWith('i'):
            case var _ when style.StartsWith('l'):
            case var _ when style.StartsWith('q'):
            case var _ when style.StartsWith("mt"):
            case var _ when style.StartsWith("is"):
            case var _ when style.StartsWith("ip"):
            case var _ when style.StartsWith("li"):
                result.Add(new ParaNode(style, children));
                break;
            case var _ when style.StartsWith("tr"):
                result.Add(new RowNode(style, children));
                break;
            case var _ when style.StartsWith("tc") || style.StartsWith("th"):
                result.Add(new CellNode(style, CellAlignment(style), TrimTrailingText(children)));
                break;
            case var _ when style.StartsWith('t'):
                result.Add(new TableNode(style, children));
                break;
            case "lb":
                result.Add(new LineBreakNode(style));
                break;
            default:
                result.Add(new AnnotationNode(style, text));
                break;
        }
    }

    private static List<IUsfmNode> LowerChildren(IEnumerable<CstNode> nodes, ReadOnlyMemory<char> source)
    {
        var concreteNodes = nodes.ToList();
        var result = new List<IUsfmNode>();
        for (var index = 0; index < concreteNodes.Count; index++)
        {
            var node = concreteNodes[index];
            if (node is CstTextNode text && index + 1 < concreteNodes.Count && concreteNodes[index + 1] is CstMarkerNode nextMarker &&
                (nextMarker.MarkerName.Span.StartsWith("tc") || nextMarker.MarkerName.Span.StartsWith("th") ||
                 nextMarker.MarkerName.Span.SequenceEqual("bk") || nextMarker.MarkerName.Span.SequenceEqual("+nd")))
            {
                var trimmed = text.Text.Span.TrimEnd();
                if (!trimmed.IsEmpty)
                    result.Add(new TextNode(trimmed.ToString()));
                continue;
            }
            LowerNode(node, source, result);
        }

        var rows = result.OfType<RowNode>().ToList();
        if (rows.Count > 0 && rows.Count == result.Count(row => row is RowNode))
            return [new TableNode(string.Empty, rows.Cast<IUsfmNode>().ToList())];

        return result;
    }

    private static List<IUsfmNode> GroupTables(List<IUsfmNode> nodes)
    {
        var result = new List<IUsfmNode>(nodes.Count);
        for (var index = 0; index < nodes.Count; index++)
        {
            if (nodes[index] is not RowNode)
            {
                result.Add(nodes[index]);
                continue;
            }

            var rows = new List<IUsfmNode>();
            while (index < nodes.Count && nodes[index] is RowNode)
                rows.Add(nodes[index++]);
            index--;
            result.Add(new TableNode(string.Empty, rows));
        }
        return result;
    }

    private static List<IUsfmNode> GroupInlineContinuations(List<IUsfmNode> nodes)
    {
        var result = new List<IUsfmNode>(nodes.Count);
        for (var index = 0; index < nodes.Count; index++)
        {
            if (nodes[index] is ParaNode paragraph && index + 1 < nodes.Count &&
                paragraph.Style.StartsWith("io", StringComparison.Ordinal) &&
                nodes[index + 1] is CharNode { Style: "ior" } continuation)
            {
                var content = paragraph.Content?.ToList() ?? [];
                if (content.LastOrDefault() is TextNode text)
                    content[^1] = new TextNode(text.Text.TrimEnd());
                content.Add(continuation);
                result.Add(new ParaNode(paragraph.Style, content));
                index++;
                continue;
            }

            result.Add(nodes[index]);
        }
        return result;
    }

    private static IList<IUsfmNode> TrimTrailingText(IList<IUsfmNode> children)
    {
        if (children.LastOrDefault() is not TextNode text)
            return children;

        var trimmed = children.ToList();
        trimmed[^1] = new TextNode(text.Text.TrimEnd());
        return trimmed;
    }

    private static bool IsNoteSubmarkerStyle(string style) =>
        style is "xo" or "xt" or "fr" or "ft" or "fq" or "fqa" or "fv" or "xk" or "xq";

    private static IList<IUsfmNode> RemoveNoteCaller(IList<IUsfmNode> children, string caller)
    {
        if (children.FirstOrDefault() is not TextNode text)
            return children;

        var remainder = text.Text.StartsWith(caller, StringComparison.Ordinal)
            ? text.Text[caller.Length..].TrimStart()
            : text.Text;
        var result = children.ToList();
        if (string.IsNullOrEmpty(remainder))
            result.RemoveAt(0);
        else
            result[0] = new TextNode(remainder);
        return result;
    }

    private static void LowerVerseContent(ImmutableArray<CstNode> nodes, ReadOnlyMemory<char> source, List<IUsfmNode> result)
    {
        var firstText = true;
        for (var index = 0; index < nodes.Length; index++)
        {
            var node = nodes[index];
            if (node is CstTextNode text)
            {
                var value = firstText
                    ? NormalizeVerseText(RemainingAfterFirstWord(text.Text.ToString()), node.Span, source)
                    : NormalizeText(text.Text.Span);
                if (!firstText && value != null && (text.Text.Span.EndsWith('\n') || text.Text.Span.EndsWith('\r')) && index + 1 < nodes.Length)
                    value += " ";
                if (!string.IsNullOrEmpty(value))
                    result.Add(new TextNode(value));
                firstText = false;
                continue;
            }

            LowerNode(node, source, result);
        }
    }

    private static UsfmAttributeCollection Attributes(IEnumerable<CstAttributeNode> nodes) =>
        new(nodes.Select(static node => new UsfmAttribute(node.Key.ToString(), node.Value.ToString(), node.Span)));

    private static string FlattenText(IEnumerable<CstNode> nodes) =>
        string.Concat(nodes.Select(static node => node switch
        {
            CstTextNode text => text.Text.ToString(),
            CstMarkerNode marker => FlattenText(marker.Children),
            _ => string.Empty
        }));

    private static string? NormalizeText(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty || value.IsWhiteSpace())
            return null;

        return value.TrimEnd("\r\n".AsSpan()).ToString();
    }

    private static string NormalizeMarkerText(string value) =>
        value.TrimEnd("\r\n".AsSpan()).ToString();

    private static string CellAlignment(string style) =>
        style.StartsWith("tcr", StringComparison.Ordinal) ? "end" : "start";

    private static string NormalizeVerseText(string value, SourceSpan markerSpan, ReadOnlyMemory<char> source)
    {
        if (!value.EndsWith('\n') && !value.EndsWith('\r'))
            return value;

        var normalized = value.TrimEnd("\r\n".AsSpan()).ToString();
        var next = source.Span[markerSpan.End..];
        return next.StartsWith("\\v".AsSpan()) ? normalized + " " : normalized;
    }

    private static string FirstWord(string value)
    {
        var span = value.AsSpan().TrimStart();
        var length = span.IndexOfAny(" \r\n\t");
        return (length < 0 ? span : span[..length]).ToString();
    }

    private static string RemainingAfterFirstWord(string value)
    {
        var span = value.AsSpan().TrimStart();
        var length = span.IndexOfAny(" \r\n\t");
        return length < 0 ? string.Empty : span[(length + 1)..].ToString().TrimStart();
    }
}