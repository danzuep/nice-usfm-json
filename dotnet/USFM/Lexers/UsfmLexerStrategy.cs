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

        // Determine if we are processing a marker block or raw text block
        token = (_remaining[0] == Backslash && _remaining.Length > 1)
            ? ParseMarkerToken()
            : ParseTextToken();

        return true;
    }

    private LexerToken ParseTextToken()
    {
        var nextSlash = _remaining.IndexOf(Backslash);
        var textSpan = nextSlash != -1 ? _remaining[..nextSlash] : _remaining;

        _remaining = nextSlash != -1 ? _remaining[nextSlash..] : ReadOnlySpan<char>.Empty;
        return new LexerToken(textSpan, ReadOnlySpan<int>.Empty);
    }

    private LexerToken ParseMarkerToken()
    {
        var originalRemaining = _remaining;
        int styleEnd = FindStyleEndIndex();
        int contentStart = styleEnd < _remaining.Length ? styleEnd + 1 : styleEnd;

        var content = _remaining[contentStart..];
        var nextBackslash = content.IndexOf(Backslash);

        // Scenario A: End of stream reached with no trailing elements
        if (nextBackslash == -1)
        {
            _remaining = ReadOnlySpan<char>.Empty;
            return new LexerToken(originalRemaining, new int[] { contentStart });
        }

        // Scenario B: Look ahead for closing style tags (\f* or milestone terminations)
        if (TryFindClosingBounds(content, originalRemaining[..contentStart], out int closeStart, out int closeEnd))
        {
            int absoluteCloseStart = contentStart + closeStart;
            int absoluteCloseEnd = contentStart + closeEnd;
            _remaining = originalRemaining[absoluteCloseEnd..];
            return new LexerToken(originalRemaining[..absoluteCloseEnd], new int[] { contentStart, absoluteCloseStart });
        }

        // Scenario C: Standard marker sequence fallthrough
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

    private bool TryFindClosingBounds(ReadOnlySpan<char> content, ReadOnlySpan<char> styleHeader, out int start, out int end)
    {
        start = end = 0;
        int nextAsterisk = content.IndexOf(Asterisk);
        if (nextAsterisk == -1) return false;

        int backslashIdx = nextAsterisk - 1;
        while (backslashIdx > 0 && content[backslashIdx] != Backslash && !char.IsWhiteSpace(content[backslashIdx]))
        {
            backslashIdx--;
        }

        if (backslashIdx < 0 || content[backslashIdx] != Backslash) return false;

        // Ensure closing syntax logically matches or relates to the opening tag style header
        var closingTag = content[backslashIdx..nextAsterisk];
        if (!closingTag.IsEmpty && styleHeader.Trim().StartsWith(closingTag))
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
}