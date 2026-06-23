namespace USFM.Lexers;

internal readonly ref struct UsfmLexerToken
{
    private readonly LexerToken _token;
    public UsfmLexerToken(LexerToken token)
    {
        _token = token;
    }

    public readonly ReadOnlySpan<char> Style =>
        UsfmLexerStrategy.GetStyle(_token[0]);

    public readonly IReadOnlyList<string> Segments => GetSegments();

    internal static UsfmLexerToken CreateSplit(LexerToken token)
    {
        var splitToken = UsfmLexerStrategy.SplitValue(token);
        return new UsfmLexerToken(splitToken);
    }

    internal static IReadOnlyList<string> CreateSegments(string input)
    {
        var tokenizer = new UsfmLexerStrategy(input.AsSpan());
        _ = tokenizer.TryMoveNext(out var token);
        return CreateSplit(token).Segments;
    }

    internal string[] GetSegments(int min = 3)
    {
        var hasClosingStar = _token.Indices.Length > 0 &&
            _token[_token.Indices.Length].TrimEnd(' ').EndsWith('*');
        var textIndex = hasClosingStar ?
            _token.Indices.Length - 1 :
            _token.Indices.Length;
        var size = Math.Max(_token.Indices.Length + 1, min);
        var result = new string[size];
        for (int i = 0; i < size; i++)
        {
            ReadOnlySpan<char> value;
            if (i == 0)
            {
                value = Style;
            }
            else if (!hasClosingStar && textIndex >= 0 && i == textIndex)
            {
                value = _token[i];
            }
            else
            {
                value = _token[i].TrimEnd(' ');
            }
            result[i] = value.ToString();
        }
        return result;
    }

    public ReadOnlySpan<char> this[int index] =>
        _token[index];

    public string Trimmed(int index) =>
        _token[index].TrimEnd(' ').ToString();

    public override string ToString() => _token.ToString();
}
