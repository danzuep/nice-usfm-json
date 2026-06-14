using static System.Net.Mime.MediaTypeNames;

namespace USFM;

public readonly ref struct UsfmToken
{
    public ReadOnlySpan<char> Type { get; }
    public ReadOnlySpan<char> Value { get; }
    public ReadOnlySpan<char> Extra { get; }
    public UsfmToken(ReadOnlySpan<char> type, ReadOnlySpan<char> value = default, ReadOnlySpan<char> extra = default)
        { Type = type; Value = value; Extra = extra; }
    public override string ToString() => $"\\{Type} {Value}{Extra}";
}

internal sealed class UsfmTokenDto
{
    public string Style { get; }
    public string Value { get; }
    public string Extra { get; }
    public UsfmTokenDto(UsfmToken usfmToken)
    {
        Style = usfmToken.Type.ToString();
        Value = usfmToken.Value.ToString();
        Extra = usfmToken.Extra.ToString();
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
    public static UsfmTokenDto Get(string input, short index = 0)
    {
        var count = 0;
        var tokenizer = new UsfmLexer(input.AsSpan());
        UsfmToken usfmToken;
        while (tokenizer.TryMoveNext(out usfmToken) && count < index)
        {
            count++;
        }
        return new UsfmTokenDto(usfmToken);
    }
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Style))
            return $"{Value}{Extra}";
        else
            return $"\\{Style} {Value}{Extra}";
    }
}