using System.Buffers;
using System.Runtime.CompilerServices;

namespace USFM.Lexers;

public ref struct UsfmLexer
{
    private ReadOnlySpan<char> _source;
    private ReadOnlySpan<char> _remaining;
    private int _offset;
    private UsfmTokenType _pendingType;
    private int _pendingOffset;
    private int _pendingLength;
    private int _pendingValueStart;
    private int _pendingValueLength;
    private bool _hasPending;
    private static readonly SearchValues<char> Delimiters = SearchValues.Create("\\|* \r\n\t");

    public UsfmLexer(ReadOnlySpan<char> input)
    {
        _source = input;
        _remaining = input;
        _offset = 0;
    }

    public bool TryMoveNext(out UsfmToken token)
    {
        if (_hasPending)
        {
            token = CreatePendingToken();
            _hasPending = false;
            return true;
        }

        if (_remaining.IsEmpty)
        {
            token = new UsfmToken(UsfmTokenType.EndOfFile, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty, _offset);
            return false;
        }

        if (_remaining[0] == '\\')
        {
            return TryParseMarker(out token);
        }

        if (_remaining[0] == '|')
        {
            token = new UsfmToken(UsfmTokenType.AttributePipe, _remaining[..1], _remaining[..1], _offset);
            Advance(1);
            return true;
        }

        return TryParseText(out token);
    }

    public bool TryPeek(out UsfmToken token)
    {
        if (_hasPending)
        {
            token = CreatePendingToken();
            return true;
        }

        if (!TryMoveNext(out token))
            return false;

        _pendingType = token.Type;
        _pendingOffset = token.Offset;
        _pendingLength = token.Span.Length;
        _pendingValueStart = token.Type is UsfmTokenType.Marker or UsfmTokenType.MarkerEnd or UsfmTokenType.MilestoneStart or UsfmTokenType.MilestoneEnd ? 1 : 0;
        _pendingValueLength = token.Value.Length;
        _hasPending = true;
        return true;
    }

    private UsfmToken CreatePendingToken() => new(
        _pendingType,
        _source.Slice(_pendingOffset, _pendingLength),
        _source.Slice(_pendingOffset + _pendingValueStart, _pendingValueLength),
        _pendingOffset);

    private void Advance(int count)
    {
        _remaining = _remaining[count..];
        _offset += count;
    }

    private bool TryParseMarker(out UsfmToken token)
    {
        int startOffset = _offset;
        var start = _remaining;

        // Skip leading backslash
        var markerSpan = _remaining[1..];
        int endOfMarker = markerSpan.IndexOfAny(Delimiters);

        if (endOfMarker == -1)
        {
            endOfMarker = markerSpan.Length;
        }

        var fullMarker = markerSpan[..endOfMarker];
        
        // Handle closing markers like \f*
        if (fullMarker.Length > 0 && endOfMarker < markerSpan.Length && markerSpan[endOfMarker] == '*')
        {
            token = new UsfmToken(UsfmTokenType.MarkerEnd, start[..(endOfMarker + 2)], fullMarker, startOffset);
            Advance(endOfMarker + 2);
            return true;
        }

        // A milestone may contain a pipe-delimited attribute list before its closing \*.
        if (fullMarker.EndsWith("-s") || fullMarker.EndsWith("-e"))
        {
            int closingOffset = _remaining.IndexOf("\\*");
            if (closingOffset >= 0)
            {
                var milestoneType = fullMarker.EndsWith("-s") ? UsfmTokenType.MilestoneStart : UsfmTokenType.MilestoneEnd;
                token = new UsfmToken(milestoneType, start[..(closingOffset + 2)], fullMarker, startOffset);
                Advance(closingOffset + 2);
                return true;
            }
        }

        token = new UsfmToken(UsfmTokenType.Marker, start[..(endOfMarker + 1)], fullMarker, startOffset);
        Advance(endOfMarker + 1);

        // Consume one trailing space if present after a marker
        if (!_remaining.IsEmpty && _remaining[0] == ' ')
        {
            Advance(1);
        }

        return true;
    }

    private bool TryParseText(out UsfmToken token)
    {
        int startOffset = _offset;
        var start = _remaining;

        int actualNext = _remaining.IndexOfAny('\\', '|');
        if (actualNext == -1)
        {
            token = new UsfmToken(UsfmTokenType.Text, start, start, startOffset);
            Advance(start.Length);
            return true;
        }

        token = new UsfmToken(UsfmTokenType.Text, _remaining[..actualNext], _remaining[..actualNext], startOffset);
        Advance(actualNext);
        return true;
    }
}
