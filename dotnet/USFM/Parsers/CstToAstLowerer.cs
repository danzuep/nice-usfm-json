using USFM.Visitors;

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
                result.Add(new TextNode(text.Text.ToString()));
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
        var text = string.Concat(children.OfType<TextNode>().Select(static child => child.Text));
        var attributes = Attributes(marker.Attributes);

        switch (style)
        {
            case "id":
                result.Add(new BookNode(style, FirstWord(text), RemainingAfterFirstWord(text)));
                break;
            case "c":
                result.Add(new ChapterNode(style, FirstWord(text)));
                break;
            case "v":
                result.Add(new VerseNode(style, FirstWord(text), RemainingAfterFirstWord(text)));
                break;
            case "w":
                result.Add(new CharNode(style, children, attributes));
                break;
            case "f":
            case "x":
                result.Add(new NoteNode(style, FirstWord(text), children));
                break;
            case var _ when style.StartsWith('p'):
                result.Add(new ParaNode(style, children));
                break;
            case var _ when style.StartsWith("tr"):
                result.Add(new RowNode(style, children));
                break;
            case var _ when style.StartsWith("tc") || style.StartsWith("th"):
                result.Add(new CellNode(style, string.Empty, children));
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
        return result;
    }

    private static Dictionary<string, string> Attributes(IEnumerable<CstAttributeNode> nodes) =>
        nodes.ToDictionary(static node => node.Key.ToString(), static node => node.Value.ToString(), StringComparer.Ordinal);

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