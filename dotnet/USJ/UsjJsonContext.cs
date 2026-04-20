using System.Text.Json.Serialization;

namespace USJ;

[JsonSerializable(typeof(UsjDocument))]
[JsonSerializable(typeof(UsjChapter))]
[JsonSerializable(typeof(UsjVerse))]
[JsonSerializable(typeof(UsjPara))]
[JsonSerializable(typeof(UsjChar))]
[JsonSerializable(typeof(UsjTable))]
[JsonSerializable(typeof(UsjRow))]
[JsonSerializable(typeof(UsjCell))]
[JsonSerializable(typeof(UsjText))]
[JsonSerializable(typeof(UsjNote))]
[JsonSerializable(typeof(UsjIdentification))]
[JsonSerializable(typeof(UsjMilestone))]
[JsonSerializable(typeof(UsjLineBreak))]
public partial class UsjJsonContext : JsonSerializerContext
{
    // This class is used to generate the JSON serialization metadata for the Usj models.
    // It allows for polymorphic serialization and deserialization of UsjNode types.
    // The [JsonSerializable] attributes specify which types should be included in the context.
}