using System.Xml;
using USJ;

namespace Usx.Parsers;

public class CharacterParser : IUsxElementParser
{
    public static readonly string Key = "char";

    public string ElementName => Key;

    public async Task<IUsjNode> ParseAsync(XmlReader reader, CancellationToken cancellationToken = default)
    {
        var style = reader.GetAttribute("style") ?? string.Empty;

        string text = string.Empty;

        if (reader.IsEmptyElement)
            return new UsjChar(Content: null, Style: style);

        await reader.ReadAsync();
        if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
        {
            text = reader.Value ?? string.Empty;
            await reader.ReadAsync();
        }

        while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await reader.ReadAsync();
        }

        return new UsjChar(Content: new List<IUsjNode> { new UsjText(text) }, Style: style);
    }
}
