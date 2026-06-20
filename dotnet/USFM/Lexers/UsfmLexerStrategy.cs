namespace USFM.Lexers;

public ref struct UsfmLexerStrategy
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
    private const char Space = ' ';

    private ReadOnlySpan<char> _remaining;
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
            token = GetMarker();
        }
        else
        {
            token = GetText();
        }

        return true;
    }

    private LexerToken GetText()
    {
        ReadOnlySpan<char> text;
        var nextSlash = _remaining.IndexOf(Backslash);

        if (nextSlash != -1)
        {
            text = _remaining[..nextSlash];
            _remaining = _remaining[nextSlash..];
        }
        else
        {
            text = _remaining;
            _remaining = ReadOnlySpan<char>.Empty;
        }

        return new LexerToken(text);
    }

    private LexerToken GetMarker()
    {
        var originalRemaining = _remaining;
        var remainingStart = GetTypeValueSplitIndex(_remaining);
        var marker = _remaining[..remainingStart];
        var remaining = _remaining[remainingStart..];

        var nextBackslash = remaining.IndexOf(Backslash);
        if (nextBackslash == -1)
        {
            var token = new LexerToken(_remaining);
            // Split 0: End of marker tag
            token.Indexes.Add(remainingStart);

            var cleanMarker = marker.TrimStart(Backslash).Trim();
            if (cleanMarker.SequenceEqual("v") || cleanMarker.SequenceEqual("id"))
            {
                var nextSpace = remaining.IndexOf(Space);
                if (nextSpace != -1)
                {
                    var splitIndex = nextSpace + 1;
                    if (splitIndex >= remaining.Length)
                    {
                        splitIndex = nextSpace;
                    }
                    // Split 1: End of value tag split
                    token.Indexes.Add(remainingStart + splitIndex);
                }
            }

            _remaining = ReadOnlySpan<char>.Empty;
            return token;
        }

        if (EndMarkerCheck(originalRemaining, remainingStart, out var endMarkerToken))
        {
            return endMarkerToken;
        }

        var cleanMarkerStr = marker.TrimStart(Backslash).Trim();
        var endMarkerIndex = remaining.IndexOf(cleanMarkerStr);
        if (endMarkerIndex >= nextBackslash && CheckChar(remaining, endMarkerIndex + cleanMarkerStr.Length))
        {
            var spanEnd = endMarkerIndex-- + cleanMarkerStr.Length;
            if (CheckNextWhiteSpace(remaining, ++spanEnd))
            {
                spanEnd++;
            }

            var totalLength = remainingStart + spanEnd;
            var token = new LexerToken(originalRemaining[..totalLength]);
            // Split 0: End of marker tag
            token.Indexes.Add(remainingStart);
            // Split 1: End of value tag / start of closing tag
            token.Indexes.Add(remainingStart + endMarkerIndex);

            _remaining = originalRemaining[totalLength..];
            return token;
        }

        var fallbackLength = remainingStart + nextBackslash;
        var fallbackToken = new LexerToken(originalRemaining[..fallbackLength]);
        // Split 0: End of marker tag
        fallbackToken.Indexes.Add(remainingStart);

        _remaining = originalRemaining[fallbackLength..];
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
        var marker = originalSpan[..remainingStart].TrimStart(Backslash).Trim();
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
        // Split 0: End of marker tag
        result.Indexes.Add(remainingStart);
        // Split 1: End of value / start of close tag
        result.Indexes.Add(remainingStart + endMarkerIndex);

        _remaining = originalSpan[totalLength..];
        return true;
    }

    private bool CheckChar(ReadOnlySpan<char> span, int index, char target = Asterisk)
        => index < span.Length && span[index] == target;

    private bool CheckNextWhiteSpace(ReadOnlySpan<char> span, int index)
        => index < span.Length && char.IsWhiteSpace(span[index]);
}