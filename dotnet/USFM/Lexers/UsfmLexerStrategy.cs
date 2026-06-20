namespace USFM.Lexers;

public ref struct UsfmLexerStrategy
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
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

        token = IsMarkerStart ? ParseMarkerToken() : ParseTextToken();
        return true;
    }

    private bool IsMarkerStart =>
        _remaining.Length > 1 && _remaining[0] == Backslash;

    private LexerToken ParseTextToken()
    {
        int nextSlash = _remaining.IndexOf(Backslash);
        if (nextSlash == -1)
        {
            var text = _remaining;
            _remaining = ReadOnlySpan<char>.Empty;
            return new LexerToken(text, ReadOnlySpan<int>.Empty);
        }

        var textSpan = _remaining[..nextSlash];
        _remaining = _remaining[nextSlash..];
        return new LexerToken(textSpan, ReadOnlySpan<int>.Empty);
    }

    private LexerToken ParseMarkerToken()
    {
        var originalRemaining = _remaining;
        int styleEnd = FindStyleEndIndex();
        int contentStart = styleEnd < _remaining.Length ? styleEnd + 1 : styleEnd;

        var content = _remaining[contentStart..];
        int nextBackslash = content.IndexOf(Backslash);

        // Scenario A: End of stream reached with no trailing elements
        if (nextBackslash == -1)
        {
            _remaining = ReadOnlySpan<char>.Empty;
            return new LexerToken(originalRemaining, new int[] { contentStart });
        }

        // Scenario B: Look ahead for closing style tags (\f* or milestone terminations like \*\ or \marker*)
        if (TryFindEndMarker(content, originalRemaining[..contentStart], out int closeStart, out int closeEnd))
        {
            int absoluteCloseStart = contentStart + closeStart;
            int absoluteCloseEnd = contentStart + closeEnd;

            _remaining = originalRemaining[absoluteCloseEnd..];
            return new LexerToken(originalRemaining[..absoluteCloseEnd], new int[] { contentStart, absoluteCloseStart });
        }

        // Scenario C: Look for matching end marker (e.g., \w ... \w*)
        var marker = originalRemaining[..contentStart].TrimEnd();
        int endMarkerIndex = content.IndexOf(marker);

        if (IsValidMatchingEndMarker(content, marker, endMarkerIndex, nextBackslash))
        {
            int spanEnd = endMarkerIndex + marker.Length + 1;
            if (spanEnd < content.Length && char.IsWhiteSpace(content[spanEnd]))
            {
                spanEnd++;
            }

            _remaining = content[spanEnd..];
            return new LexerToken(originalRemaining[..(contentStart + spanEnd)], new int[] { contentStart, contentStart + endMarkerIndex });
        }

        // Scenario D: Standard marker sequence fallthrough
        int fallbackLength = contentStart + nextBackslash;
        _remaining = originalRemaining[fallbackLength..];
        return new LexerToken(originalRemaining[..fallbackLength], new int[] { contentStart });
    }

    private int FindStyleEndIndex()
    {
        int index = 1;
        while (index < _remaining.Length && !char.IsWhiteSpace(_remaining[index]))
        {
            index++;
        }
        return index;
    }

    private bool TryFindEndMarker(ReadOnlySpan<char> content, ReadOnlySpan<char> styleHeader, out int start, out int end)
    {
        start = end = 0;

        int nextAsterisk = content.IndexOf(Asterisk);
        if (nextAsterisk == -1) return false;

        // Walk backward from the asterisk to find the preceding backslash
        int backslashIdx = nextAsterisk - 1;
        while (backslashIdx >= 0 && content[backslashIdx] != Backslash && !char.IsWhiteSpace(content[backslashIdx]))
        {
            backslashIdx--;
        }

        if (backslashIdx < 0 || content[backslashIdx] != Backslash) return false;

        // Extract the closing tag (text between backslash and asterisk)
        var closingTag = nextAsterisk > backslashIdx + 1
            ? content[(backslashIdx + 1)..nextAsterisk]
            : ReadOnlySpan<char>.Empty;

        var openingTag = styleHeader.TrimStart(Backslash).TrimEnd();

        // Match if closing tag matches opening tag, OR standalone \* for specific milestone markers
        bool isStandAloneMilestone = closingTag.IsEmpty &&
            (openingTag.StartsWith("qt-e") || openingTag.EndsWith("-s") || openingTag.EndsWith("-e"));

        if (openingTag.SequenceEqual(closingTag) || isStandAloneMilestone)
        {
            start = backslashIdx;
            end = nextAsterisk + 1;

            if (end < content.Length && char.IsWhiteSpace(content[end]))
            {
                end++;
            }
            return true;
        }

        return false;
    }

    private static bool IsValidMatchingEndMarker(ReadOnlySpan<char> content, ReadOnlySpan<char> marker, int endMarkerIndex, int nextBackslash)
    {
        return endMarkerIndex >= nextBackslash &&
               endMarkerIndex > 0 &&
               endMarkerIndex + marker.Length < content.Length &&
               content[endMarkerIndex + marker.Length] == Asterisk;
    }
}