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

    private bool TryGetNext(char value, out int index)
    {
        index = _remaining.IndexOf(value);
        return index >= 0;
    }

    private LexerToken ParseTextToken()
    {
        ReadOnlySpan<char> textSpan;
        if (TryGetNext(Backslash, out var nextBackslash))
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
        int contentStart = styleEndIndex < originalRemaining.Length
            ? styleEndIndex + 1
            : styleEndIndex;

        var contentAfterHeader = originalRemaining[contentStart..];
        int nextBackslashInContent = contentAfterHeader.IndexOf(Backslash);

        // Scenario A: Marker at end of input
        if (nextBackslashInContent == -1)
        {
            _remaining = ReadOnlySpan<char>.Empty;
            return new LexerToken(originalRemaining, new[] { contentStart });
        }

        var styleHeader = originalRemaining[..styleEndIndex];

        // Scenario B: Look for closing marker (generic: matching \tag* or milestone \*)
        if (TryFindClosingMarker(contentAfterHeader, styleHeader, out int relativeCloseStart, out int relativeCloseEnd))
        {
            int absoluteCloseStart = contentStart + relativeCloseStart;
            int absoluteCloseEnd = contentStart + relativeCloseEnd;

            _remaining = originalRemaining[absoluteCloseEnd..];
            return new LexerToken(
                originalRemaining[..absoluteCloseEnd],
                new[] { contentStart, absoluteCloseStart });
        }

        // Scenario C: Inline repeating end-marker (e.g. \w ... \w*)
        var trimmedHeader = styleHeader.Trim();
        int endMarkerStart = contentAfterHeader.IndexOf(trimmedHeader);
        if (IsValidInlineEndMarker(contentAfterHeader, trimmedHeader, endMarkerStart, nextBackslashInContent))
        {
            int spanEnd = endMarkerStart + trimmedHeader.Length + 1; // +1 for *
            if (spanEnd < contentAfterHeader.Length && char.IsWhiteSpace(contentAfterHeader[spanEnd]))
            {
                spanEnd++;
            }

            int absoluteSpanEnd = contentStart + spanEnd;
            _remaining = originalRemaining[absoluteSpanEnd..];
            return new LexerToken(
                originalRemaining[..absoluteSpanEnd],
                new[] { contentStart, contentStart + endMarkerStart });
        }

        // Scenario D: Fallback - single marker up to next marker
        int fallbackEnd = contentStart + nextBackslashInContent;
        _remaining = originalRemaining[fallbackEnd..];
        return new LexerToken(originalRemaining[..fallbackEnd], new[] { contentStart });
    }

    private int FindStyleEndIndex()
    {
        int index = 1; // Skip initial backslash
        while (index < _remaining.Length && !char.IsWhiteSpace(_remaining[index]))
        {
            index++;
        }
        return index;
    }

    /// <summary>
    /// Generic closing marker detection supporting both:
    /// - Standard: \tag ... \tag*
    /// - Milestone/self-closing style: \tag ... \*
    /// </summary>
    private bool TryFindClosingMarker(
        ReadOnlySpan<char> content,
        ReadOnlySpan<char> styleHeader,
        out int start,
        out int end)
    {
        start = end = 0;

        // Prefer exact matching closing tag first (\tag*)
        var trimmedHeader = styleHeader.TrimStart(Backslash).TrimEnd();
        if (!trimmedHeader.IsEmpty)
        {
            // Simple search for \ + header + *
            // (This is efficient enough for typical USFM; could be optimized further if needed)
            int candidate = 0;
            while ((candidate = content[candidate..].IndexOf(Backslash)) >= 0)
            {
                candidate += content.Length - content[candidate..].Length; // Adjust absolute
                int tagEnd = candidate + 1 + trimmedHeader.Length;
                if (tagEnd < content.Length
                    && content[(candidate + 1)..tagEnd].SequenceEqual(trimmedHeader)
                    && content[tagEnd] == Asterisk)
                {
                    start = candidate;
                    end = tagEnd + 1;
                    if (end < content.Length && char.IsWhiteSpace(content[end]))
                        end++;
                    return true;
                }
                candidate++; // Continue searching after this backslash
            }
        }

        // Fallback: Milestone-style standalone \* (common for -s/-e milestones and self-closing)
        int asteriskIndex = content.IndexOf(Asterisk);
        if (asteriskIndex > 0)
        {
            int backslashIndex = asteriskIndex - 1;
            if (content[backslashIndex] == Backslash)
            {
                start = backslashIndex;
                end = asteriskIndex + 1;

                if (end < content.Length && char.IsWhiteSpace(content[end]))
                    end++;

                return true;
            }
        }

        return false;
    }

    private static bool IsValidInlineEndMarker(
        ReadOnlySpan<char> content,
        ReadOnlySpan<char> marker,
        int endMarkerIndex,
        int nextBackslash)
    {
        return endMarkerIndex >= nextBackslash
            && endMarkerIndex > 0
            && endMarkerIndex + marker.Length < content.Length
            && content[endMarkerIndex + marker.Length] == Asterisk;
    }

    internal static ReadOnlySpan<char> GetStyle(ReadOnlySpan<char> rawType)
    {
        return rawType.TrimStart(Backslash).TrimEnd(Space).TrimEnd(Asterisk);
    }
}