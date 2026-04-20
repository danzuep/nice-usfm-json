using System.Xml;
using USJ;

namespace Usx.Parsers;

public class BookParser : IUsxElementParser
{
    public static readonly string Key = "book";

    public string ElementName => Key;

    public async Task<IUsjNode> ParseAsync(XmlReader reader, CancellationToken cancellationToken = default)
    {
        var style = reader.GetAttribute("style") ?? string.Empty;
        var code = reader.GetAttribute("code") ?? string.Empty;
        string versionName = string.Empty;

        if (reader.IsEmptyElement)
        {
            return new UsjIdentification(code, versionName, null, style);
        }

        await reader.ReadAsync();
        if (reader.NodeType == XmlNodeType.Text)
        {
            versionName = reader.Value ?? string.Empty;
            await reader.ReadAsync();
        }

        while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await reader.ReadAsync();
        }

        return new UsjIdentification(code, versionName, null, style);
    }
}
