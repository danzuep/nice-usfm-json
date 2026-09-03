using USFM.Visitors;

namespace USFM.Parsers;

public sealed class UsfmReader
{
    public async IAsyncEnumerable<IUsfmNode> ReadAsync(
        Stream source,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var reader = new StreamReader(source, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        foreach (var node in Parse(text))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return node;
        }
    }

    private static IReadOnlyList<IUsfmNode> Parse(string source) => UsfmParser.Parse(source.AsSpan());
}