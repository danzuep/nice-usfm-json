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

    internal static UsfmLexerToken Create(LexerToken token, bool splitValue = false)
    {
        if (splitValue)
        {
            var splitToken = UsfmLexerStrategy.SplitValue(token);
            return new UsfmLexerToken(splitToken);
        }

        return new UsfmLexerToken(token);
    }

    private string[] GetSegments()
    {
        var hasClosingStar = _token.Indices.Length > 0 &&
            _token[_token.Indices.Length].Trim(' ').EndsWith('*');
        var textIndex = hasClosingStar ?
            _token.Indices.Length - 1 :
            _token.Indices.Length;
        var size = Math.Max(_token.Indices.Length + 1, 3);
        var result = new string[size];
        for (int i = 0; i < size; i++)
        {
            ReadOnlySpan<char> value;
            if (i == 0)
            {
                value = Style;
            }
            else if (textIndex >= 0 && i == textIndex)
            {
                value = _token[i];
            }
            else
            {
                value = _token[i].Trim(' ');
            }
            result[i] = value.ToString();
        }
        return result;
    }

    public override string ToString() => _token.ToString();
}
