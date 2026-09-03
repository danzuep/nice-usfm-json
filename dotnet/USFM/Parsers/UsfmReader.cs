using USFM.Ast;

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
        var nodes = CstToAstLowerer.Parse(text.AsMemory(), out _);
        foreach (var node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return node;
        }
    }

}