using System.Text.Json.Serialization;

namespace USJ;

// The new USJ spec uses a "type" discriminator with variable values
// (e.g. "para:p", "char:bk", "cell:th1") and inlines text as raw strings.
[JsonConverter(typeof(UsjNodeConverter))]
public interface IUsjNode
{
    string Type { get; }
}

public interface IUsjStyleNode : IUsjNode
{
    string? Style { get; }
}

public interface IUsjContentNode : IUsjNode
{
    IList<IUsjNode>? Content { get; }
}

public static class UsjConstants
{
    public static readonly IReadOnlyList<string> ParaStylesToHide = ["ide", "toc", "mt"];
}