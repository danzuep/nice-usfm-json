namespace USFM;

public enum UsfmTokenType { Marker, Text }

public readonly ref struct UsfmToken
{
    public UsfmTokenType Type { get; }
    public ReadOnlySpan<char> Value { get; }
    public UsfmToken(UsfmTokenType type, ReadOnlySpan<char> value)
    {
        Type = type;
        Value = value;
    }
}

public ref struct UsfmTokenizer
{
    private ReadOnlySpan<char> _remaining;
    public UsfmTokenizer(ReadOnlySpan<char> input) => _remaining = input;

    public bool MoveNext(out UsfmToken token)
    {
        if (_remaining.IsEmpty)
        {
            token = default;
            return false;
        }

        // Tokenize Markers (\v, \va, \va*)
        if (_remaining[0] == '\\')
        {
            int index = 1;
            while (index < _remaining.Length &&
                   (char.IsLetterOrDigit(_remaining[index]) || _remaining[index] == '*'))
            {
                index++;
            }

            var marker = _remaining.Slice(1, index - 1);
            _remaining = _remaining.Slice(index);

            if (!_remaining.IsEmpty && _remaining[0] == ' ')
                _remaining = _remaining.Slice(1); // Drop trailing delimiter space

            token = new UsfmToken(UsfmTokenType.Marker, marker);
            return true;
        }

        // Tokenize Text Content
        int nextSlash = _remaining.IndexOf('\\');
        if (nextSlash == -1)
        {
            var text = _remaining;
            _remaining = ReadOnlySpan<char>.Empty;
            token = new UsfmToken(UsfmTokenType.Text, text);
            return true;
        }
        else
        {
            var text = _remaining.Slice(0, nextSlash);
            _remaining = _remaining.Slice(nextSlash);
            token = new UsfmToken(UsfmTokenType.Text, text);
            return true;
        }
    }
}