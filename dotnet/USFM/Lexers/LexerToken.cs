namespace USFM.Lexers;

public readonly ref struct LexerToken
{
    public readonly ReadOnlySpan<char> Span;
    public readonly ReadOnlySpan<int> Indices;

    public LexerToken(ReadOnlySpan<char> span, ReadOnlySpan<int> indices)
    {
        Span = span;
        Indices = indices;
    }

    public static LexerToken Empty => new LexerToken(ReadOnlySpan<char>.Empty, default);

    public ReadOnlySpan<char> this[int index]
    {
        get
        {
            if (index < 0 || index > Indices.Length) return ReadOnlySpan<char>.Empty;

            int start = index == 0 ? 0 : Indices[index - 1];
            int end = index == Indices.Length ? Span.Length : Indices[index];

            return Span.Slice(start, end - start);
        }
    }

    public IReadOnlyList<string> GetSegments()
    {
        var result = new List<string>();
        for (int i = 0; i <= Indices.Length; i++)
        {
            result.Add(this[i].ToString());
        }
        return result;
    }

    public override string ToString() => Span.ToString();
}
