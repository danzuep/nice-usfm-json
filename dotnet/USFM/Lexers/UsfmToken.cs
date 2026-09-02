namespace USFM.Lexers;

public enum UsfmTokenType
{
    None,
    Text,
    Marker,         // \v, \p, etc.
    MarkerEnd,      // \v*, \f*, etc.
    AttributePipe,  // |
    AttributeKey,   // lemma=
    AttributeValue, // "G1234"
    MilestoneStart, // \qt-s\*
    MilestoneEnd,   // \qt-e\*
    EndOfFile
}

public readonly ref struct UsfmToken
{
    public UsfmTokenType Type { get; init; }
    public ReadOnlySpan<char> Span { get; init; }
    public ReadOnlySpan<char> Value { get; init; }
    public int Offset { get; init; }

    public UsfmToken(UsfmTokenType type, ReadOnlySpan<char> span, ReadOnlySpan<char> value, int offset)
    {
        Type = type;
        Span = span;
        Value = value.IsEmpty ? span : value;
        Offset = offset;
    }

    public static UsfmToken Empty => new(UsfmTokenType.None, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty, 0);
}
