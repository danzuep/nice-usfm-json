namespace USFM.Lexers;

public readonly ref struct LexerToken
{
    public IList<int> Indexes { get; } = new List<int>();
    public ReadOnlySpan<char> Span { get; }
    public LexerToken(ReadOnlySpan<char> span = default) => Span = span;
    public override string ToString() => Span.ToString();
}