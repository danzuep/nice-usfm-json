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

        var markerStr = marker.ToString();
        // (no special-casing for wrapper markers here) nested inline markers should be
        // emitted by the tokenizer so the parser can build CharNode wrappers.
        // Special handling for attribute-based milestones (e.g. qt-s, ts-s) where we need to include
        // the "\\*" delimiter and any following inline text as part of the token value.
        if ((markerStr.EndsWith("-s") || markerStr.EndsWith("-e")) && !remaining.IsEmpty)
        {
            var close = remaining.IndexOf("\\*");
            if (close != -1)
            {
                // include up to the next marker after the closing delimiter, if any
                var afterClose = remaining.Slice(close + 2);
                var nextAfterClose = afterClose.IndexOf(Backslash);
                var includeLen = (nextAfterClose != -1) ? (close + 2 + nextAfterClose) : remaining.Length;
                token = new UsfmToken(marker, remaining[..includeLen]);
                _remaining = (includeLen < remaining.Length) ? remaining.Slice(includeLen) : ReadOnlySpan<char>.Empty;
                return;
            }
        }

        // Special handling for footnote markers: include the entire footnote block up to the
        // closing "\f*" so the parser can treat the footnote payload as a single value.
        if (markerStr == "f" && !remaining.IsEmpty)
        {
            var closeF = remaining.IndexOf("\\f*");
            if (closeF != -1)
            {
                // include through the closing marker itself and any following inline text up to next marker
                var afterClose = remaining.Slice(closeF + 3);
                var nextAfterClose = afterClose.IndexOf(Backslash);
                var includeLen = (nextAfterClose != -1) ? (closeF + 3 + nextAfterClose) : remaining.Length;
                token = new UsfmToken(marker, remaining[..includeLen]);
                _remaining = (includeLen < remaining.Length) ? remaining.Slice(includeLen) : ReadOnlySpan<char>.Empty;
                return;
            }
        }

        var nextSlash = remaining.IndexOf(Backslash);
        if (nextSlash != -1)
        {
            // include the whitespace immediately preceding the next marker so that when parser
            // appends adjacent TextNodes the original spacing is preserved
            var valueSpan = remaining[..nextSlash];
            token = new UsfmToken(marker, valueSpan);
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
