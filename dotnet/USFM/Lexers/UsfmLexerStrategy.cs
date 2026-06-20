namespace USFM.Lexers;

public ref struct UsfmLexerStrategy : ILexerStrategy
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
    private const char Space = ' ';

    private readonly ReadOnlySpan<char> _remaining;
    public UsfmLexerStrategy(ReadOnlySpan<char> remaining) =>
        _remaining = remaining;

    public bool TryMoveNext(out LexerToken token)
    {
        if (_remaining.IsEmpty)
        {
            token = default;
            return false;
        }

        if (_remaining[0] == Backslash && _remaining.Length > 1)
        {
            token = GetMarker(_remaining);
        }
        else
        {
            token = GetText(_remaining);
        }

        return true;
    }

    private LexerToken GetText(ReadOnlySpan<char> span)
    {
        ReadOnlySpan<char> text;
        var nextSlash = span.IndexOf(Backslash);

        if (nextSlash != -1)
        {
            text = span[..nextSlash];
            span = span[nextSlash..];
        }
        else
        {
            text = span;
            span = ReadOnlySpan<char>.Empty;
        }

        return new LexerToken(text);
    }

    private LexerToken GetMarker(ReadOnlySpan<char> span)
    {
        var startIndex = GetTypeValueSplitIndex(span);
        var marker = span[..startIndex];
        var remaining = span[startIndex..];

        var nextBackslash = remaining.IndexOf(Backslash);
        if (nextBackslash == -1)
        {
            var token = new LexerToken(span);
            token.Indexes.Add(startIndex); // Index 0: End of marker tag

            if (marker.SequenceEqual("v") || marker.SequenceEqual("id"))
            {
                var nextSpace = remaining.IndexOf(Space);
                if (nextSpace != -1)
                {
                    var splitIndex = nextSpace + 1;
                    if (splitIndex >= remaining.Length)
                    {
                        splitIndex = nextSpace;
                    }
                    token.Indexes.Add(startIndex + splitIndex); // Index 1: End of value split
                }
            }

            span = ReadOnlySpan<char>.Empty;
            return token;
        }

        if (EndMarkerCheck(span, startIndex, out var endMarkerToken))
        {
            span = span[endMarkerToken.Span.Length..];
            return endMarkerToken;
        }

        var endMarkerIndex = remaining.IndexOf(marker);
        if (endMarkerIndex >= nextBackslash && CheckChar(remaining, endMarkerIndex + marker.Length))
        {
            var spanEnd = endMarkerIndex-- + marker.Length;
            if (CheckNextWhiteSpace(remaining, ++spanEnd))
            {
                spanEnd++;
            }

            var totalLength = startIndex + spanEnd;
            var token = new LexerToken(span[..totalLength]);
            token.Indexes.Add(startIndex);               // Index 0: End of marker tag
            token.Indexes.Add(startIndex + endMarkerIndex); // Index 1: End of attributes / start of closing marker

            span = span[totalLength..];
            return token;
        }

        var fallbackLength = startIndex + nextBackslash;
        var fallbackToken = new LexerToken(span[..fallbackLength]);
        fallbackToken.Indexes.Add(startIndex); // Index 0: End of marker tag

        span = span[fallbackLength..];
        return fallbackToken;
    }

    private int GetTypeValueSplitIndex(ReadOnlySpan<char> span)
    {
        var index = 1;
        while (index < span.Length && !char.IsWhiteSpace(span[index]))
        {
            index++;
        }

        var remainingStart = index + 1;
        return remainingStart >= span.Length ? index : remainingStart;
    }

    private bool EndMarkerCheck(ReadOnlySpan<char> originalSpan, int remainingStart, out LexerToken result)
    {
        var marker = originalSpan[..remainingStart];
        var span = originalSpan[remainingStart..];
        var nextAsterisk = span.IndexOf(Asterisk);

        if (nextAsterisk == -1)
        {
            result = default;
            return false;
        }

        var endMarkerIndex = nextAsterisk - 1;
        while (endMarkerIndex > 0 &&
               !span[endMarkerIndex].Equals(Backslash) &&
               !char.IsWhiteSpace(span[endMarkerIndex]))
        {
            endMarkerIndex--;
        }

        var startMarker = span[endMarkerIndex..nextAsterisk];
        if (!startMarker.IsEmpty && !marker.StartsWith(startMarker))
        {
            result = default;
            return false;
        }

        var spanEnd = nextAsterisk + 1;
        if (spanEnd >= span.Length)
        {
            spanEnd = nextAsterisk;
        }

        if (char.IsWhiteSpace(span[spanEnd]))
        {
            spanEnd++;
        }
        else if (spanEnd == span.Length - 1)
        {
            spanEnd++;
        }

        var totalLength = remainingStart + spanEnd;
        result = new LexerToken(originalSpan[..totalLength]);
        result.Indexes.Add(remainingStart); // Index 0: End of marker tag
        result.Indexes.Add(remainingStart + endMarkerIndex); // Index 1: End of value / start of close tag

        return true;
    }

    private bool CheckChar(ReadOnlySpan<char> span, int index, char target = Asterisk)
        => index < span.Length && span[index] == target;

    private bool CheckNextWhiteSpace(ReadOnlySpan<char> span, int index)
        => index < span.Length && char.IsWhiteSpace(span[index]);
}