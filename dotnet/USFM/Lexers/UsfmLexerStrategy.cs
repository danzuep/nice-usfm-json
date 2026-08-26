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
            token = LexerToken.Empty;
            return false;
        }

        token = IsMarkerStart ?
            ParseMarkerToken() :
            ParseTextToken();
        return true;
    }

    private bool IsMarkerStart =>
        _remaining.Length > 1 && _remaining[0] == Backslash;

    private static bool TryGetNext(ReadOnlySpan<char> span, char value, out int index)
    {
        index = span.IndexOf(value);
        return index >= 0;
    }

    private LexerToken ParseTextToken()
    {
        ReadOnlySpan<char> textSpan;
        if (TryGetNext(_remaining, Backslash, out var nextBackslash))
        {
            textSpan = _remaining[..nextBackslash];
            _remaining = _remaining[nextBackslash..];
        }
        else
        {
            textSpan = _remaining;
            _remaining = ReadOnlySpan<char>.Empty;
        }
        var token = new LexerToken(textSpan, ReadOnlySpan<int>.Empty);
        return token;
    }

    private LexerToken ParseMarkerToken()
    {
        var originalRemaining = _remaining;
        int styleEndIndex = FindStyleEndIndex();
        int contentStart = styleEndIndex < originalRemaining.Length ?
            styleEndIndex + 1 :
            styleEndIndex;

        if (contentStart < originalRemaining.Length && originalRemaining[contentStart - 1] != ' ')
        {
            while (contentStart < originalRemaining.Length && char.IsWhiteSpace(originalRemaining[contentStart]))
            {
                contentStart++;
            }
        }

        var content = originalRemaining[contentStart..];
        var nextBackslash = content.IndexOf(Backslash);

        // Scenario A: End of stream reached with no trailing elements
        if (nextBackslash == -1)
        {
            _remaining = ReadOnlySpan<char>.Empty;
            return new LexerToken(originalRemaining, new[] { contentStart });
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
        while (backslashIdx >= 0 && content[backslashIdx] != Backslash)
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

    internal static LexerToken SplitValue(LexerToken token, char splitChar = Space)
    {
        var indices = token.Indices;
        if (TryGetNext(token[1], splitChar, out var nextSpace))
        {
            var indicesArray = indices.ToArray();
            var lastIndex = indicesArray.Length;
            Array.Resize(ref indicesArray, lastIndex + 1);
            var previousMax = lastIndex > 0 ? indicesArray[lastIndex - 1] : 0;
            indicesArray[lastIndex] = ++nextSpace + previousMax;
            indices = indicesArray;
        }
        return new LexerToken(token.Span, indices);
    }

    internal static ReadOnlySpan<char> GetStyle(ReadOnlySpan<char> rawType)
    {
        return rawType.TrimStart(Backslash).TrimEnd().TrimEnd(Asterisk).TrimEnd();
    }
}