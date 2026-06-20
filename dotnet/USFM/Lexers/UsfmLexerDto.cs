namespace USFM.Lexers;

internal readonly struct UsfmLexerDto
{
    public static UsfmLexerDto Empty => new UsfmLexerDto(Array.Empty<UsfmLexerTokenDto>());

    private readonly IReadOnlyList<UsfmLexerTokenDto> _tokens;

    public UsfmLexerDto(IReadOnlyList<UsfmLexerTokenDto> tokens)
    {
        _tokens = tokens;
    }

    public static async Task<IReadOnlyList<UsfmLexerDto>> TokenizeAsync(StreamReader reader, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var result = new List<UsfmLexerDto>();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            var tokens = UsfmLexerTokenDto.Tokenize(line);
            result.Add(new UsfmLexerDto(tokens));
        }
        return result;
    }

    public override string ToString() =>
        string.Concat(_tokens.Select(t => t.ToString()));
}