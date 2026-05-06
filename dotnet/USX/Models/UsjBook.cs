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
        IList<IUsjNode>? Content = null,
        string? Style = null
    ) : UsjContentNodeBase("book", Style, Content)
    {
        public UsjBook() : this(string.Empty, new UsjIdentification(), new List<IUsjNode>()) { }
    }


}
