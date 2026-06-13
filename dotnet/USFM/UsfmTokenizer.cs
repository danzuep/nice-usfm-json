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
        while (index < _remaining.Length && _remaining[index] != Asterisk && !char.IsWhiteSpace(_remaining[index]))
        {
            index++;
        }

        // include trailing asterisk as part of the marker (e.g. \w*)
        var includeAsterisk = (index < _remaining.Length && _remaining[index] == Asterisk);
        var markerEnd = index + (includeAsterisk ? 1 : 0);
        var marker = _remaining[1..markerEnd];

        // Determine start of the value: skip a single whitespace after the marker (or asterisk) if present
        var remainingStart = markerEnd;
        if (remainingStart < _remaining.Length && char.IsWhiteSpace(_remaining[remainingStart]))
            remainingStart++;

        var remaining = _remaining[remainingStart..];
        // Trim any leading whitespace from the value
        while (!remaining.IsEmpty && char.IsWhiteSpace(remaining[0]))
            remaining = remaining.Slice(1);

        var nextSlash = remaining.IndexOf(Backslash);
        if (nextSlash != -1)
        {
            token = new UsfmToken(marker, remaining[..nextSlash]);
            _remaining = remaining.Slice(nextSlash);
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
