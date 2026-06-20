namespace USFM.Lexers;

public ref struct UsfmLexerStrategy
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
    private const char Space = ' ';
    private ReadOnlySpan<char> _remaining;

    public UsfmLexerStrategy(ReadOnlySpan<char> remaining)
    {
        _remaining = remaining;
    }

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
        return new LexerToken(text, ReadOnlySpan<int>.Empty);
    }

    private LexerToken GetMarker()
    {
        var originalRemaining = _remaining;

        // Extract Type-Value base split boundaries
        var index = 1;
        while (index < _remaining.Length && !char.IsWhiteSpace(_remaining[index]))
        {
            index++;
        }

        var remainingStart = index + 1;
        if (remainingStart >= _remaining.Length)
        {
            remainingStart = index;
        }

        var marker = _remaining[0..remainingStart];
        var remaining = _remaining[remainingStart..];

        var nextBackslash = remaining.IndexOf(Backslash);
        if (nextBackslash == -1)
        {
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
                    _remaining = ReadOnlySpan<char>.Empty;
                    return new LexerToken(originalRemaining, new int[] { remainingStart, remainingStart + splitIndex });
                }
            }

            _remaining = ReadOnlySpan<char>.Empty;
            return new LexerToken(originalRemaining, new int[] { remainingStart });
        }

        if (EndMarkerCheck(marker, remaining, remainingStart, originalRemaining, out var endToken))
        {
            return endToken;
        }

        var endMarkerIndex = remaining.IndexOf(marker);
        if (endMarkerIndex >= nextBackslash &&
            endMarkerIndex + marker.Length < remaining.Length &&
            remaining[endMarkerIndex + marker.Length] == Asterisk)
        {
            var origEndMarkerIndex = endMarkerIndex;
            var spanEnd = endMarkerIndex + marker.Length;
            endMarkerIndex--;
            spanEnd++;
            if (spanEnd < remaining.Length && char.IsWhiteSpace(remaining[spanEnd]))
            {
                spanEnd++;
            }
            var totalLength = remainingStart + spanEnd;
            _remaining = originalRemaining[totalLength..];
            return new LexerToken(originalRemaining[..totalLength], new int[] { remainingStart, remainingStart + endMarkerIndex });
        }

        var fallbackLength = remainingStart + nextBackslash;
        _remaining = originalRemaining[fallbackLength..];
        return new LexerToken(originalRemaining[..fallbackLength], new int[] { remainingStart });
    }

    private bool EndMarkerCheck(ReadOnlySpan<char> marker, ReadOnlySpan<char> span, int remainingStart, ReadOnlySpan<char> originalSpan, out LexerToken result)
    {
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
        _remaining = originalSpan[totalLength..];
        result = new LexerToken(originalSpan[..totalLength], new int[] { remainingStart, remainingStart + endMarkerIndex });
        return true;
    }
}