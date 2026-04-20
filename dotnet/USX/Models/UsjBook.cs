using System.Text.Json.Serialization;
using USJ;

namespace USX.Models
{
    public sealed record UsjBook(
        [property: JsonIgnore]
    string SchemaVersion,
        [property: JsonPropertyName("metadata")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    UsjIdentification? Metadata = null,
        [property: JsonPropertyName("content")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IList<IUsjNode>? Content = null
    ) : UsjNodeBase("book"), IUsjContentNode
    {
        public override string Type => "book";
        public UsjBook() : this(string.Empty, new UsjIdentification(), new List<IUsjNode>()) { }
    }
}
