using System.Text.Json;
using System.Text.Json.Serialization;
using USFM.Visitors;
using USJ;

namespace USFM;

public class UsfmConverter
{
    public async Task<string> ConvertUsfmToMarkdownAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var visitor = new MarkdownConvertingVisitor();
        await visitor.ParseAsync(usfmStream, cancellationToken);
        return visitor.FinalizeResult();
    }

    public async Task<string> ConvertUsfmToYamlAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var visitor = new YamlConvertingVisitor();
        await visitor.ParseAsync(usfmStream, cancellationToken);
        return visitor.GetResult();
    }

    public async Task<IList<IUsfmNode>> ConvertUsfmAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var visitor = new UsfmStructuredVisitor();
        await visitor.ParseAsync(usfmStream, cancellationToken);
        var content = new List<IUsfmNode>();
        content.AddRange(visitor.FinalizeResult());
        return content;
    }

    public async Task<UsjDocument> ConvertUsfmToUsjAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var visitor = new UsjConvertingVisitor();
        await visitor.ParseAsync(usfmStream, cancellationToken);
        var content = new List<IUsjNode>();
        content.AddRange(visitor.FinalizeResult());
        return new UsjDocument { Content = [.. content] };
    }

    public async Task<string> ConvertUsfmToUsjJsonAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        var document = await ConvertUsfmToUsjAsync(usfmStream, cancellationToken);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(document, options);
    }
}