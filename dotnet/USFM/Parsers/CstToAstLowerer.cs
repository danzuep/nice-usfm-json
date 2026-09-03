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
        return result;
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
        var style = marker.MarkerName.ToString();
        var children = LowerChildren(marker.Children, source);
        var text = FlattenText(marker.Children);
        var attributes = Attributes(marker.Attributes);
        if (style == "w" && attributes.Remove("default", out var shorthand))
            attributes["lemma"] = shorthand;
        if (style == "ca" && attributes.Count == 0)
            attributes["status"] = "invalid";

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
                var verseText = NormalizeVerseText(RemainingAfterFirstWord(text), marker.Span, source.Length);
                result.Add(new VerseNode(style, FirstWord(text), string.IsNullOrEmpty(verseText) ? null : verseText));
                if (!string.IsNullOrEmpty(verseText))
                    result.Add(new TextNode(verseText));
                break;
            case "w":
            case "add":
            case "ca":
            case "nd":
            case "ord":
            case "pn":
            case "ior":
            case "va":
            case "vp":
                result.Add(new CharNode(style, children, attributes));
                break;
            case "f":
            case "x":
                result.Add(new NoteNode(style, FirstWord(text), children));
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
                result.Add(new CellNode(style, CellAlignment(style), children));
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
        var result = new List<IUsfmNode>();
        foreach (var node in nodes)
            LowerNode(node, source, result);

        var rows = result.OfType<RowNode>().ToList();
        if (rows.Count > 0 && rows.Count == result.Count(row => row is RowNode))
            return [new TableNode("table", rows.Cast<IUsfmNode>().ToList())];

        return result;
    }

    private static Dictionary<string, string> Attributes(IEnumerable<CstAttributeNode> nodes) =>
        nodes.ToDictionary(static node => node.Key.ToString(), static node => node.Value.ToString(), StringComparer.Ordinal);

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

    private static string NormalizeVerseText(string value, SourceSpan markerSpan, int sourceLength)
    {
        if (!value.EndsWith('\n') && !value.EndsWith('\r'))
            return value;

        var normalized = value.TrimEnd("\r\n".AsSpan()).ToString();
        return markerSpan.End < sourceLength ? normalized + " " : normalized;
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