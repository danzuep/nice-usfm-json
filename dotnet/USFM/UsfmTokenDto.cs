namespace USFM;

internal sealed record UsfmTokenDto(string Type, string Value, string Extra)
{
    public UsfmTokenDto(UsfmToken usfmToken) : this(
        usfmToken.Type.ToString(),
        usfmToken.Value.ToString(),
        usfmToken.Extra.ToString())
    {
    }

    public string Style => Type.TrimStart('\\').TrimEnd(' ').TrimEnd('*');

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

    public override string ToString() => $"{Type}{Value}{Extra}";
}