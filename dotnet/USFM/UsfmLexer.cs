namespace USFM;

public ref struct UsfmLexer
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
    private const char Space = ' ';
    private ReadOnlySpan<char> _remaining;
    public UsfmLexer(ReadOnlySpan<char> input) => _remaining = input;

    public bool TryMoveNext(out UsfmToken token)
    {
        if (_remaining.IsEmpty)
        {
            token = default;
            return false;
        }

        if (_remaining[0] == Backslash && _remaining.Length > 1)
        {
            GetMarker(out token);
        }
        else
        {
            GetText(out token);
        }

        return true;
    }

    internal static UsfmToken SplitValue(UsfmToken token, char splitChar = Space)
    {
        UsfmToken result;
        var input = token.Value;
        var nextSpace = input.IndexOf(splitChar);
        if (nextSpace != -1)
        {
            var text = input[..nextSpace];
            var remaining = input[(nextSpace + 1)..];
            result = new UsfmToken(token.Type, text, remaining);
        }
        else
        {
            result = new UsfmToken(token.Type, input);
        }
        return result;
    }

    private void GetMarker(out UsfmToken token)
    {
        GetTypeValueSplit(out var backslashToken);
        var marker = backslashToken.Type;
        var remaining = backslashToken.Value;

        var nextBackslash = remaining.IndexOf(Backslash);
        if (nextBackslash == -1)
        {
            if (marker.SequenceEqual("v") || marker.SequenceEqual("id"))
            {
                token = SplitValue(backslashToken);
            }
            else
            {
                token = backslashToken;
            }
            _remaining = ReadOnlySpan<char>.Empty;
            return;
        }

        if (EndMarkerCheck(backslashToken, nextBackslash, out token))
        {
            return;
        }
        var endMarkerIndex = remaining.IndexOf(marker);
        if (endMarkerIndex >= nextBackslash &&
            CheckChar(remaining, endMarkerIndex + marker.Length))
        {
            var spanEnd = endMarkerIndex-- + marker.Length;
            if (CheckNextWhiteSpace(remaining, ++spanEnd))
            {
                spanEnd++;
            }
            var valueSpan = remaining[..endMarkerIndex];
            var extraSpan = remaining[endMarkerIndex..spanEnd];
            token = new UsfmToken(marker, valueSpan, extraSpan);
            _remaining = remaining.Slice(spanEnd);
            return;
        }

        token = new UsfmToken(marker, remaining[..nextBackslash]);
        _remaining = remaining.Slice(nextBackslash);
    }

    private bool CheckChar(ReadOnlySpan<char> span, int index, char target = Asterisk)
    {
        return index < span.Length && span[index] == target;
    }

    private bool CheckNextWhiteSpace(ReadOnlySpan<char> span, int index)
    {
        return index < span.Length && char.IsWhiteSpace(span[index]);
    }

    private bool EndMarkerCheck(UsfmToken original, int nextBackslash, out UsfmToken result)
    {
        var span = original.Value;
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

        // Return early if the end marker doesn't match the start marker
        var startMarker = span[endMarkerIndex..nextAsterisk];
        if (!startMarker.IsEmpty && !original.Type.StartsWith(startMarker))
        {
            result = default;
            return false;
        }

        var spanEnd = nextAsterisk + 1;
        if (spanEnd >= _remaining.Length)
        {
            spanEnd = nextAsterisk;
        }

        if (char.IsWhiteSpace(span[spanEnd]))
        {
            spanEnd++;
        }

        var valueSpan = span[..endMarkerIndex];
        var extraSpan = span[endMarkerIndex..spanEnd];
        result = new UsfmToken(original.Type, valueSpan, extraSpan);
        _remaining = span.Slice(spanEnd);
        return true;
    }

    private void GetTypeValueSplit(out UsfmToken token)
    {
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
        token = new UsfmToken(marker, remaining);
    }

    private void GetText(out UsfmToken token)
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
        token = new UsfmToken(default, text);
    }
}
