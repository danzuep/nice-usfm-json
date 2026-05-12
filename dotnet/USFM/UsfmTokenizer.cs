namespace USFM;

public ref struct UsfmTokenizer
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
    private ReadOnlySpan<char> _remaining;
    public UsfmTokenizer(ReadOnlySpan<char> input) => _remaining = input;

    public bool TryMoveNext(out UsfmToken token)
    {
        if (_remaining.IsEmpty)
        {
            token = default;
            return false;
        }

        if (_remaining[0] == Backslash)
        {
            GetMarker(out token);
        }
        else
        {
            GetText(out token);
        }

        return true;
    }

    private void GetMarker(out UsfmToken token)
    {
        var index = 1;
        while (index < _remaining.Length &&
            _remaining[index] != Asterisk &&
            !char.IsWhiteSpace(_remaining[index]))
        {
            index++;
        }
        var marker = _remaining[1..index];
        if (index < _remaining.Length)
        {
            index++;
        }
        var remaining = _remaining[index..];
        var nextSlash = remaining.IndexOf(Backslash);
        if (nextSlash != -1)
        {
            var nextIndex = nextSlash == 0 ? nextSlash : nextSlash - 1;
            token = new UsfmToken(marker, remaining[..nextIndex]);
            _remaining = remaining[nextSlash..];
        }
        else
        {
            token = new UsfmToken(marker, remaining);
            _remaining = ReadOnlySpan<char>.Empty;
        }
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
