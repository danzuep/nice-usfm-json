namespace USFM;

public ref struct UsfmTokenizer
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
    private const char Space = ' ';
    private ReadOnlySpan<char> _remaining;
    public UsfmTokenizer(ReadOnlySpan<char> input) => _remaining = input;

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
            if (token.Type.SequenceEqual("v") || token.Type.SequenceEqual("id"))
            {
                token = SplitValue(token);
            }
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
        var index = 1;
        while (index < _remaining.Length && !char.IsWhiteSpace(_remaining[index]))
        {
            index++;
        }

        var remainingStart = index + 1;
        if (remainingStart == _remaining.Length)
            remainingStart = index;

        var marker = _remaining[1..index];
        var remaining = _remaining[remainingStart..];

        var nextBackslash = remaining.IndexOf(Backslash);
        if (nextBackslash == -1)
        {
            token = new UsfmToken(marker, remaining);
            _remaining = ReadOnlySpan<char>.Empty;
            return;
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
            if (nextBackslash == backslashBeforeAsterisk)
            {
                var valueSpan = remaining[..nextBackslash];
                var asteriskSlice = remaining.Slice(nextAsterisk);
                var anotherBackslash = asteriskSlice.IndexOf(Backslash);
                var extraIndex = anotherBackslash != -1 ? anotherBackslash : 1;
                if (anotherBackslash == -1)
                {
                    var nextSpace = asteriskSlice.IndexOf(Space);
                    if (nextSpace != -1)
                    {
                        extraIndex += nextSpace;
                    }
                }
                var extraSpanEnd = nextAsterisk + extraIndex;
                var extraSpan = remaining[nextBackslash..extraSpanEnd];
                token = new UsfmToken(marker, valueSpan, extraSpan);
                _remaining = remaining.Slice(extraSpanEnd);
                return;
            }
        }

        token = new UsfmToken(marker, remaining[..nextBackslash]);
        _remaining = remaining.Slice(nextBackslash);
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
