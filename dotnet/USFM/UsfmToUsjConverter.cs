using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using USJ;

namespace USFM;

public class UsfmToUsjConverter
{
    private const string UsjVersion = "0.0.1-alpha.2";

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

    public async Task<UsjDocument> ConvertUsfmToUsjAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(usfmStream);
        var content = await ParseUsfmDocumentAsync(reader, cancellationToken);
        return new UsjDocument
        {
            Version = UsjVersion,
            Content = [.. content]
        };
    }

    public async Task<IList<IUsjNode>> ParseUsfmDocumentAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        var content = new List<IUsjNode>();
        await foreach (var nodes in EnumerateUsfmDocumentAsync(reader, cancellationToken))
        {
            content.AddRange(nodes);
        }
        return content;
    }

    public async IAsyncEnumerable<IReadOnlyList<IUsjNode>> EnumerateUsfmDocumentAsync(TextReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            yield return ParseLine(line);
        }
    }

    private IReadOnlyList<IUsjNode> ParseLine(string line)
    {
        // Convert string to ReadOnlySpan to begin zero-allocation processing
        return UsjSpanParser.ParseLineToUsj(line.AsSpan());
    }
}