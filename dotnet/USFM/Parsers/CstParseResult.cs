namespace USFM.Parsers;

public sealed record CstParseResult(
    ReadOnlyMemory<char> Source,
    CstRootNode Cst,
    IReadOnlyList<ParsingDiagnostic> Diagnostics);