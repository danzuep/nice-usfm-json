using USFM.Ast;

namespace USFM.Parsers;

public sealed record UsfmParseResult(
    ReadOnlyMemory<char> Source,
    CstRootNode Cst,
    IReadOnlyList<IUsfmNode> Ast,
    IReadOnlyList<ParsingDiagnostic> Diagnostics,
    SourceMap SourceMap);