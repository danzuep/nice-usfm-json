namespace USFM.Lexers;

internal sealed record LexerTokenDto(string FullText, IReadOnlyList<string> Segments)
{
    /// <summary>
    /// Materializes a stack-allocated LexerToken into a heap-safe DTO.
    /// Handles any number of split indexes dynamically.
    /// </summary>
    public LexerTokenDto(LexerToken token) : this(
        token.Span.ToString(),
        ExtractSegments(token))
    {
    }

    private static IReadOnlyList<string> ExtractSegments(LexerToken token)
    {
        var segments = new List<string>();
        var span = token.Span;
        var indexes = token.Indexes;

        int prev = 0;
        foreach (int idx in indexes)
        {
            // Bounding guard to protect against out-of-order or out-of-bounds indices
            int current = Math.Clamp(idx, prev, span.Length);
            segments.Add(span[prev..current].ToString());
            prev = current;
        }

        // Catch any remaining characters trailing after the final split index
        if (prev <= span.Length)
        {
            segments.Add(span[prev..].ToString());
        }

        return segments;
    }

    /// <summary>
    /// Safe indexer to fetch any segment without risking an IndexOutOfRangeException.
    /// </summary>
    public string this[int index] =>
        index >= 0 && index < Segments.Count ? Segments[index] : string.Empty;

    /// <summary>
    /// High-performance tokenization driver that works for ANY strategy type.
    /// </summary>
    public static IReadOnlyList<LexerTokenDto> Tokenize<TLexer>(ref TLexer lexer)
        where TLexer : ILexerStrategy, allows ref struct
    {
        var tokens = new List<LexerTokenDto>();
        while (lexer.TryMoveNext(out var token))
        {
            tokens.Add(new LexerTokenDto(token));
        }
        return tokens;
    }

    public override string ToString() => FullText;
}