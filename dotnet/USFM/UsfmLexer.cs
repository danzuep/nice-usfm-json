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
        else
        {
            var endMarker = remaining.IndexOf(marker);
            if (endMarker > nextBackslash &&
                CheckChar(remaining, endMarker + marker.Length))
            {
                var spanEnd = endMarker-- + marker.Length;
                if (CheckNextWhiteSpace(remaining, ++spanEnd))
                {
                    spanEnd++;
                }
                var valueSpan = remaining[..endMarker];
                var extraSpan = remaining[endMarker..spanEnd];
                token = new UsfmToken(marker, valueSpan, extraSpan);
                _remaining = remaining.Slice(spanEnd);
                return;
            }
            else
            {
                token = new UsfmToken(marker, remaining[..nextBackslash]);
                _remaining = remaining.Slice(nextBackslash);
            }
        }

        var nextAsterisk = remaining.IndexOf(Asterisk);
        if (nextAsterisk != -1)
        {
            var backslashBeforeAsterisk = nextAsterisk - 1;
            while (backslashBeforeAsterisk > 0 &&
                !remaining[backslashBeforeAsterisk].Equals(Backslash) &&
                !char.IsWhiteSpace(remaining[backslashBeforeAsterisk]))
            {
                backslashBeforeAsterisk--;
            }
            var extraSpanEnd = nextAsterisk + 1;
            if (nextBackslash == backslashBeforeAsterisk && extraSpanEnd < remaining.Length)
            {
                var valueSpan = remaining[..nextBackslash];
                var isWhiteSpaceNext = char.IsWhiteSpace(remaining[extraSpanEnd]);
                if (isWhiteSpaceNext && extraSpanEnd + 1 < remaining.Length)
                {
                    extraSpanEnd++;
                }
                var extraSpan = remaining[nextBackslash..extraSpanEnd];
                token = new UsfmToken(marker, valueSpan, extraSpan);
                _remaining = remaining.Slice(extraSpanEnd);
                return;
            }
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

    private void GetTypeValueSplit(out UsfmToken token)
    {
        var index = 1;
        while (index < _remaining.Length && !char.IsWhiteSpace(_remaining[index]))
        {
            index++;
        }

        var remainingStart = index + 1;
        if (remainingStart >= _remaining.Length)
            remainingStart = index;

        var marker = _remaining[1..index];
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
