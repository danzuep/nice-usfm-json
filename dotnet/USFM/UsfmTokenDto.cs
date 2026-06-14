namespace USFM;

internal sealed record UsfmTokenDto(string Style, string Value, string Extra)
{
    public UsfmTokenDto(UsfmToken usfmToken) : this(
        usfmToken.Type.ToString(),
        usfmToken.Value.ToString(),
        usfmToken.Extra.ToString())
    {
    }

    public static IReadOnlyList<UsfmTokenDto> Tokenize(string input)
    {
        var tokens = new List<UsfmTokenDto>();
        var tokenizer = new UsfmLexer(input.AsSpan());
        while (tokenizer.TryMoveNext(out var usfmToken))
        {
            tokens.Add(new UsfmTokenDto(usfmToken));
        }
        return tokens;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Style))
            return $"{Value}{Extra}";
        else
            return $"\\{Style} {Value}{Extra}";
    }
}