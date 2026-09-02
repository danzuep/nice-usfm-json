namespace USFM.Parsers;

public readonly record struct SourceSpan(int Start, int Length)
{
    public static SourceSpan Empty => new(0, 0);
    
    public int End => Start + Length;

    public ReadOnlySpan<char> GetSpan(ReadOnlySpan<char> source) => 
        source.Slice(Start, Length);
}

public enum DiagnosticLevel
{
    Info,
    Warning,
    Error
}

public readonly record struct ParsingDiagnostic(
    string Message,
    SourceSpan Span,
    DiagnosticLevel Level = DiagnosticLevel.Error
);
