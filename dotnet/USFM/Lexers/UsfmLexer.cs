using System.Buffers;
using System.Runtime.CompilerServices;

namespace USFM.Lexers;

public ref struct UsfmLexer
{
    private ReadOnlySpan<char> _remaining;
    private int _offset;
    private static readonly SearchValues<char> Delimiters = SearchValues.Create("\\|* \r\n\t");

    public UsfmLexer(ReadOnlySpan<char> input)
    {
        _remaining = input;
        _offset = 0;
    }

    public bool TryMoveNext(out UsfmToken token)
    {
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
        if (fullMarker.Length > 0 && fullMarker[^1] == '*')
        {
            var markerName = fullMarker[..^1];
            token = new UsfmToken(UsfmTokenType.MarkerEnd, start[..(endOfMarker + 1)], markerName, startOffset);
            Advance(endOfMarker + 1);
            return true;
        }

        // Check if it's a milestone like \qt-s\* or \qt-e\*
        if (_remaining.Length > endOfMarker + 2 && _remaining[endOfMarker + 1] == '\\' && _remaining[endOfMarker + 2] == '*')
        {
            var milestoneType = fullMarker.EndsWith("-s") ? UsfmTokenType.MilestoneStart : 
                               fullMarker.EndsWith("-e") ? UsfmTokenType.MilestoneEnd : UsfmTokenType.Marker;
            
            if (milestoneType != UsfmTokenType.Marker)
            {
                token = new UsfmToken(milestoneType, start[..(endOfMarker + 3)], fullMarker, startOffset);
                Advance(endOfMarker + 3);
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
