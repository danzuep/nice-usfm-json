namespace USFM.Parsers;

public sealed class SourceMap
{
    private readonly Dictionary<int, SourceSpan> _spans = new();

    public void Add(int nodeId, SourceSpan span) => _spans[nodeId] = span;

    public bool TryGetSpan(int nodeId, out SourceSpan span) => _spans.TryGetValue(nodeId, out span);

    public IReadOnlyDictionary<int, SourceSpan> Spans => _spans;
}