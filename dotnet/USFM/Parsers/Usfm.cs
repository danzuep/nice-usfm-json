using System.Diagnostics;
using USFM.Ast;
using USFM.Visitors;
using USJ;

namespace USFM.Parsers;

public static class Usfm
{
    public static readonly ActivitySource ActivitySource = new("USFM");

    public static CstRootNode ParseCst(ReadOnlyMemory<char> source)
        => ParseCstResult(source).Cst;

    public static CstParseResult ParseCstResult(ReadOnlyMemory<char> source)
    {
        using var activity = ActivitySource.StartActivity("usfm.parse-cst");
        activity?.SetTag("usfm.source.length", source.Length);
        var parser = new UsfmCstParser(source);
        var cst = parser.Parse();
        return new CstParseResult(source, cst, parser.Diagnostics.ToArray());
    }

    public static UsfmParseResult ParseAst(ReadOnlyMemory<char> source)
    {
        using var activity = ActivitySource.StartActivity("usfm.parse-ast");
        activity?.SetTag("usfm.source.length", source.Length);
        var parsedCst = ParseCstResult(source);
        var cst = parsedCst.Cst;
        var ast = CstToAstLowerer.Lower(cst, source);
        var sourceMap = SourceMapBuilder.Build(cst);
        activity?.SetTag("usfm.diagnostics.count", parsedCst.Diagnostics.Count);
        activity?.SetTag("usfm.ast.node_count", ast.Count);
        return new UsfmParseResult(source, cst, ast, parsedCst.Diagnostics, sourceMap);
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