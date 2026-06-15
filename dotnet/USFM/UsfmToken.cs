namespace USFM;

public readonly ref struct UsfmToken
{
    public ReadOnlySpan<char> Type { get; }
    public ReadOnlySpan<char> Value { get; }
    public ReadOnlySpan<char> Extra { get; }
    public UsfmToken(ReadOnlySpan<char> type, ReadOnlySpan<char> value = default, ReadOnlySpan<char> extra = default)
        { Type = type; Value = value; Extra = extra; }
    public override string ToString() => $"{Type}{Value}{Extra}";
}