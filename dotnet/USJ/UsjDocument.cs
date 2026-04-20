using System.Text.Json;
using System.Text.Json.Serialization;

namespace USJ;

public sealed record UsjDocument(
    [property: JsonPropertyName("version")]
    string Version,
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("USJ", Style, Content)
{
    public UsjDocument() : this(string.Empty) { }
}

public sealed record UsjIdentification(
    [property: JsonPropertyName("code")]
    string TranslationCode,
    [property: JsonPropertyName("description")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? VersionDescription = null,
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("book", Style ?? "id", Content)
{
    public UsjIdentification() : this(string.Empty) { }
}

public sealed record UsjChapter(
    string Number,
    string? StartId = null,
    string? Style = null
) : UsjMarkerBase(Number, "chapter", Style ?? "c", StartId)
{
    public UsjChapter() : this(string.Empty) { }
}

public sealed record UsjVerse(
    string Number,
    string? StartId = null,
    string? Style = null
) : UsjMarkerBase(Number, "verse", Style ?? "v", StartId)
{
    public UsjVerse() : this(string.Empty) { }
}

public sealed record UsjPara(
    [property: JsonPropertyName("vid")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Vid = null,
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("para", Style ?? "p", Content)
{
    [JsonIgnore]
    public string? Text => Content == null ? null
        : string.Concat(Content.OfType<UsjText>().Select(t => t.Text));
}

public sealed record UsjChar(
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("char", Style, Content)
{
    /// <summary>
    /// All unknown attributes (status, lemma, closed, link-id, x-myattr, or any future key) are captured here.
    /// </summary>
    [JsonPropertyOrder(0)]
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtraProperties { get; init; } = new();

    [JsonIgnore]
    public string? Text => Content == null ? null
        : string.Concat(Content.OfType<UsjText>().Select(t => t.Text));
}

public sealed record UsjTable(
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("table", Style, Content)
{
}

public sealed record UsjRow(
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("row", Style ?? "tr", Content)
{
}

public sealed record UsjCell(
    [property: JsonPropertyName("align")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Align = null,
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("cell", Style, Content)
{
}

public sealed record UsjText(
    [property: JsonPropertyName("text")]
    string Text
) : UsjNodeBase("text")
{
    public UsjText() : this(string.Empty) { }
}

public sealed record UsjCrossReference : UsjNote
{
    public UsjCrossReference() : this(string.Empty, null) { }
    public UsjCrossReference(string caller, IList<IUsjNode>? content = null)
        : base(caller, content, "x") { }
}

public sealed record UsjFootnote : UsjNote
{
    public UsjFootnote() : this(string.Empty, null) { }
    public UsjFootnote(string caller, IList<IUsjNode>? content = null)
        : base(caller, content, "f") { }
}

public record UsjNote(
    [property: JsonPropertyName("caller")]
    string Caller,
    IList<IUsjNode>? Content = null,
    string? Style = null
) : UsjContentNodeBase("note", Style, Content)
{
    public UsjNote() : this(string.Empty) { }
}

public sealed record UsjMilestone(
    string? StartId = null,
    string? EndId = null,
    [property: JsonPropertyOrder(int.MaxValue)]
    [property: JsonPropertyName("who")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Who = null,
    string? Style = null
) : UsjStartEndBase("ms", Style, StartId, EndId)
{
}

public sealed record UsjLineBreak(
    string? Style = null
) : UsjNodeBase("lb", Style)
{
}

public abstract record UsjNodeBase(
    [property: JsonIgnore]
    string NodeKey,
    [property: JsonIgnore]
    string? Style = null
) : IUsjStyleNode
{
    [JsonPropertyOrder(-1)]
    [JsonPropertyName("type")]
    public virtual string Type =>
        string.IsNullOrEmpty(Style) ?
            NodeKey : $"{NodeKey}:{Style}";
}

public abstract record UsjContentNodeBase(
    string NodeKey,
    string? Style = null,
    [property: JsonPropertyOrder(int.MaxValue)]
    [property: JsonPropertyName("content")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IList<IUsjNode>? Content = null
) : UsjNodeBase(NodeKey, Style), IUsjContentNode
{
}

public abstract record UsjStartEndBase(
    string NodeKey,
    string? Style = null,
    [property: JsonPropertyName("sid")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? StartId = null,
    [property: JsonPropertyName("eid")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? EndId = null
) : UsjNodeBase(NodeKey, Style)
{
}

public abstract record UsjMarkerBase(
    [property: JsonPropertyName("number")]
    string Number,
    string NodeKey,
    string? Style = null,
    string? StartId = null
) : UsjStartEndBase(NodeKey, Style, StartId)
{
}
