using USFM.Visitors;

namespace USFM;

public class UsfmToMarkdownConverter
{
    public async Task<string> ConvertUsfmToMarkdownAsync(Stream usfmStream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(usfmStream);
        var content = await ParseUsfmDocumentAsync(reader, cancellationToken);
        return content;
    }

    public async Task<string> ParseUsfmDocumentAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        MarkdownConvertingVisitor visitor = new();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            visitor.Accept(line);
        }
        return visitor.GetResult();
    }
}