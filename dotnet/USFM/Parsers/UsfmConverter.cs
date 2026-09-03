using System.Text.Json;
using USFM.Visitors;
using USJ;

namespace USFM.Parsers;

public class UsfmConverter
{
    public async Task<string> ConvertUsfmToMarkdownAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var visitor = new MarkdownConvertingVisitor();
        var parsed = await ParseAsync(usfmStream, cancellationToken);
        visitor.Accept(parsed.Ast);
        return visitor.FinalizeResult();
    }

    public async Task<string> ConvertUsfmToYamlAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var visitor = new YamlConvertingVisitor();
        var parsed = await ParseAsync(usfmStream, cancellationToken);
        visitor.Accept(parsed.Ast);
        return visitor.GetResult();
    }

    public static async Task<IList<IUsfmNode>> ConvertUsfmAsync(StreamReader usfmReader, CancellationToken cancellationToken = default)
    {
        var source = await usfmReader.ReadToEndAsync(cancellationToken);
        return CstToAstLowerer.Parse(source.AsMemory(), out _).ToList();
    }

    public async Task<IList<IUsfmNode>> ConvertUsfmAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        return (await ParseAsync(usfmStream, cancellationToken)).Ast.ToList();
    }

    public async Task<UsjDocument> ConvertUsfmToUsjAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var parsed = await ParseAsync(usfmStream, cancellationToken);
        var visitor = new UsjConvertingVisitor();
        visitor.Accept(parsed.Ast);
        return new UsjDocument { Content = [.. visitor.FinalizeResult()] };
    }

    public async Task<string> ConvertUsfmToUsjJsonAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var document = await ConvertUsfmToUsjAsync(usfmStream, cancellationToken);
        return JsonSerializer.Serialize(document, UsjJsonContext.Default.UsjDocument);
    }

    private static async Task<UsfmParseResult> ParseAsync(Stream source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var reader = new StreamReader(source, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return Usfm.ParseAst(text.AsMemory());
    }
}