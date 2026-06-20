namespace USFM.Lexers;

public readonly ref struct LexerToken
{
    public static LexerToken Empty => new LexerToken(ReadOnlySpan<char>.Empty, default);

    public readonly ReadOnlySpan<char> Span;
    public readonly ReadOnlySpan<int> Indices;

    public LexerToken(ReadOnlySpan<char> span, ReadOnlySpan<int> indices = default)
    {
        Span = span;
        Indices = indices;
    }

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

    public override string ToString() => Span.ToString();
}
