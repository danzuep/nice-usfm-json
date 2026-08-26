namespace USFM.Lexers;

internal readonly ref struct UsfmLexerToken
{
    private readonly LexerToken _token;
    public UsfmLexerToken(LexerToken token)
    {
        _token = token;
    }

    internal UsfmLexerToken SplitValue()
    {
        var splitToken = UsfmLexerStrategy.SplitValue(_token);
        return new UsfmLexerToken(splitToken);
    }

    public readonly int Count =>
        _token.Indices.Length;

    public readonly ReadOnlySpan<char> Style =>
        UsfmLexerStrategy.GetStyle(_token[0]);

    public readonly ReadOnlySpan<char> First =>
        Count > 1 ? _token[1] : ReadOnlySpan<char>.Empty;

    public readonly ReadOnlySpan<char> Last =>
        Count > 0 ? _token[Count] : ReadOnlySpan<char>.Empty;

    public readonly ReadOnlySpan<char> Content =>
        HasClosingStar ? First : First.TrimEnd();

    public readonly ReadOnlySpan<char> Extra =>
        HasClosingStar ? Last.TrimEnd() : Last;

    public readonly bool HasClosingStar =>
        Count > 0 && _token[Count].TrimEnd().EndsWith('*');

    public readonly IReadOnlyList<string> Segments => GetSegments();

    internal string[] GetSegments(int min = 3)
    {
        var textIndex = HasClosingStar ? Count - 1 : Count;
        var size = Math.Max(Count + 1, min);
        var result = new string[size];
        for (int i = 0; i < size; i++)
        {
            ReadOnlySpan<char> value;
            if (i == 0)
            {
                value = Style;
            }
            else if (!HasClosingStar && textIndex >= 0 && i == textIndex)
            {
                value = _token[i].TrimEnd("\r\n".AsSpan());
            }
            else
            {
                value = _token[i].TrimEnd();
            }
            result[i] = value.ToString();
        }
        return result;
    }

    public ReadOnlySpan<char> this[int index] =>
        _token[index];

    public string Trimmed(int index) =>
        _token[index].TrimEnd().ToString();

    public override string ToString() => _token.ToString();
}
