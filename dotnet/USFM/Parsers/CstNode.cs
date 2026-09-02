using System.Collections.Immutable;

namespace USFM.Parsers;

public enum CstNodeType
{
    Root,
    Marker,
    MarkerEnd,
    Text,
    Attribute,
    Milestone,
    Whitespace
}

public abstract record CstNode(SourceSpan Span);

public sealed record CstRootNode(SourceSpan Span, ImmutableArray<CstNode> Children) : CstNode(Span);

public sealed record CstMarkerNode(
    SourceSpan Span, 
    ReadOnlyMemory<char> MarkerName, 
    ImmutableArray<CstNode> Children) : CstNode(Span);

public sealed record CstTextNode(SourceSpan Span, ReadOnlyMemory<char> Text) : CstNode(Span);

public sealed record CstAttributeNode(
    SourceSpan Span, 
    ReadOnlyMemory<char> Key, 
    ReadOnlyMemory<char> Value) : CstNode(Span);

public sealed record CstMilestoneNode(
    SourceSpan Span,
    ReadOnlyMemory<char> MarkerName,
    bool IsEnd,
    ImmutableArray<CstNode> Attributes) : CstNode(Span);
