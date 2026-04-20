using System.Text.Json;
using System.Text.Json.Serialization;

namespace USJ;

/// <summary>
/// Custom converter that powers the new USJ spec (unchanged behavior for all existing nodes).
/// Only change: UsjChar is now ultra-simple + fully extensible via JsonExtensionData.
/// </summary>
public class UsjNodeConverter : JsonConverter<IUsjNode>
{
    public override IUsjNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new UsjText(reader.GetString() ?? string.Empty);
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject or String for USJ node, got {reader.TokenType}");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
        {
            throw new JsonException("USJ node is missing required 'type' property");
        }

        var nodeType = typeElement.GetString();
        if (string.IsNullOrEmpty(nodeType))
        {
            throw new JsonException("USJ node has invalid 'type' value");
        }

        var nodeTypes = nodeType.Split([':'], 2);
        var nodeKey = nodeTypes[0];
        var nodeStyle = nodeTypes.Length > 1 ? nodeTypes[1] : null;

        static string GetStringProperty(JsonElement el, string name) =>
            el.GetProperty(name).GetString() ?? string.Empty;

        static string? TryGetStringProperty(JsonElement el, string name) =>
            el.TryGetProperty(name, out var p) ? p.GetString() : null;

        static IList<IUsjNode>? GetContent(JsonElement el, string propName, JsonSerializerOptions opts)
        {
            if (!el.TryGetProperty(propName, out var cEl) || cEl.ValueKind != JsonValueKind.Array)
                return null;
            return JsonSerializer.Deserialize<IList<IUsjNode>>(cEl.GetRawText(), opts);
        }

        // NEW helper – captures every unknown key/value on a char node
        static Dictionary<string, JsonElement> GetExtraProperties(JsonElement el)
        {
            var extras = new Dictionary<string, JsonElement>();
            var known = new HashSet<string> { "type", "content", "status", "lemma", "closed", "link-id", "x-myattr" };

            foreach (var prop in el.EnumerateObject())
            {
                if (!known.Contains(prop.Name))
                {
                    extras[prop.Name] = prop.Value.Clone();
                }
            }
            return extras;
        }

        // Simplified switch (UsjChar case is the only one that changed)
        return nodeKey switch
        {
            "USJ" => new UsjDocument(
                GetStringProperty(root, "version"),
                GetContent(root, "content", options),
                Style: nodeStyle),

            "book" => new UsjIdentification(
                GetStringProperty(root, "code"),
                TryGetStringProperty(root, "description"),
                GetContent(root, "content", options),
                Style: nodeStyle),

            "chapter" => new UsjChapter(
                GetStringProperty(root, "number"),
                TryGetStringProperty(root, "sid"),
                Style: nodeStyle),

            "verse" => new UsjVerse(
                GetStringProperty(root, "number"),
                TryGetStringProperty(root, "sid"),
                Style: nodeStyle),

            "para" => new UsjPara(
                TryGetStringProperty(root, "vid"),
                GetContent(root, "content", options),
                Style: nodeStyle),

            "char" => new UsjChar(
                GetContent(root, "content", options),
                Style: nodeStyle)
                {
                    ExtraProperties = GetExtraProperties(root)
                },

            "table" => new UsjTable(
                GetContent(root, "content", options),
                Style: nodeStyle),

            "row" => new UsjRow(
                GetContent(root, "content", options),
                Style: nodeStyle),

            "cell" => new UsjCell(
                TryGetStringProperty(root, "align"),
                GetContent(root, "content", options),
                Style: nodeStyle),

            "note" => new UsjNote(
                GetStringProperty(root, "caller"),
                GetContent(root, "content", options),
                Style: nodeStyle),

            "ms" => new UsjMilestone(
                TryGetStringProperty(root, "sid"),
                TryGetStringProperty(root, "eid"),
                TryGetStringProperty(root, "who"),
                Style: nodeStyle),

            "lb" => new UsjLineBreak(
                Style: nodeStyle),

            _ => throw new JsonException($"Unknown USJ node type: {nodeType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, IUsjNode value, JsonSerializerOptions options)
    {
        if (value is UsjText textNode)
        {
            writer.WriteStringValue(textNode.Text);
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}