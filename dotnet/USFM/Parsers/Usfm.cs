using System.Diagnostics;
using USFM.Visitors;
using USJ;

namespace USFM.Parsers;

public static class Usfm
{
    public static readonly ActivitySource ActivitySource = new("USFM");

    public static CstRootNode ParseCst(ReadOnlyMemory<char> source)
    {
        using var activity = ActivitySource.StartActivity("usfm.parse-cst");
        activity?.SetTag("usfm.source.length", source.Length);
        var parser = new UsfmCstParser(source);
        return parser.Parse();
    }

    public static UsfmParseResult ParseAst(ReadOnlyMemory<char> source)
    {
        using var activity = ActivitySource.StartActivity("usfm.parse-ast");
        activity?.SetTag("usfm.source.length", source.Length);
        var parser = new UsfmCstParser(source);
        var cst = parser.Parse();
        var ast = CstToAstLowerer.Lower(cst, source);
        var sourceMap = SourceMapBuilder.Build(cst);
        activity?.SetTag("usfm.diagnostics.count", parser.Diagnostics.Count);
        activity?.SetTag("usfm.ast.node_count", ast.Count);
        return new UsfmParseResult(source, cst, ast, parser.Diagnostics.ToArray(), sourceMap);
    }

    public static UsjDocument ParseUsj(ReadOnlyMemory<char> source)
    {
        using var activity = ActivitySource.StartActivity("usfm.project-usj");
        var parsed = ParseAst(source);
        var visitor = new UsjConvertingVisitor();
        visitor.Accept(parsed.Ast);
        return new UsjDocument { Content = [.. visitor.FinalizeResult()] };
    }

    private static class SourceMapBuilder
    {
        public static SourceMap Build(CstRootNode root)
        {
            var map = new SourceMap();
            var id = 0;
            Add(root, map, ref id);
            return map;
        }

        private static void Add(CstNode node, SourceMap map, ref int id)
        {
            map.Add(id++, node.Span);
            if (node is CstRootNode root)
            {
                foreach (var child in root.Children)
                    Add(child, map, ref id);
            }
            else if (node is CstMarkerNode marker)
            {
                foreach (var child in marker.Children)
                    Add(child, map, ref id);
            }
        }
    }
}