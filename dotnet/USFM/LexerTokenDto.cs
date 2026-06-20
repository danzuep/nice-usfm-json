using USFM.Lexers;

namespace USFM;

internal readonly struct LexerTokenDto
{
    public readonly string Raw;
    public readonly string Style;
    public readonly IReadOnlyList<string> Segments;

    public LexerTokenDto(LexerToken token)
    {
        Raw = token.Span.ToString();
        Style = GetStyle(token[0]);
        Segments = GetSegments(token);
    }

    public static IReadOnlyList<LexerTokenDto> Tokenize(params string[] input)
    {
        var result = new List<LexerTokenDto>();
        var text = string.Concat(input);
        var tokenizer = new UsfmLexerStrategy(text.AsSpan());
        while (tokenizer.TryMoveNext(out var token))
        {
            result.Add(new LexerTokenDto(token));
        }
        return result;
    }

    private static IReadOnlyList<string> GetSegments(LexerToken token)
    {
        var result = new List<string>();
        for (int i = 0; i <= token.Indices.Length; i++)
        {
            result.Add(token[i].ToString());
        }
        return result;
    }

    private static string GetStyle(ReadOnlySpan<char> rawType)
    {
        return UsfmLexerStrategy.GetStyle(rawType).ToString();
    }

    public override string ToString() => Raw;
}