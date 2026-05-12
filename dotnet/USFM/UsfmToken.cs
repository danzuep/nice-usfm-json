namespace USFM;

public readonly ref struct UsfmToken
{
    public ReadOnlySpan<char> Type { get; }
    public ReadOnlySpan<char> Value { get; }
    public UsfmToken(ReadOnlySpan<char> type, ReadOnlySpan<char> value = default)
        { Type = type; Value = value; }
    public override string ToString() => $"{Type} {Value}";
}