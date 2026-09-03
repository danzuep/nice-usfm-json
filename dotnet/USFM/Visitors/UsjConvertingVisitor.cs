using System.Text.Json;
using USFM.Ast;
using USJ;

namespace USFM.Visitors;

public class UsjConvertingVisitor : AstProjectionVisitor<IUsjNode>
{
    protected override IUsjNode CreateBook(BookNode node) =>
        new UsjBook(node.Code, null,
            node.Description == null ? null : [new UsjText(node.Description)], node.Style);

    protected override IUsjNode CreateChapter(ChapterNode node, string startId) =>
        new UsjChapter(node.Number, startId, node.Style);

    protected override IUsjNode CreateVerse(VerseNode node, string startId) =>
        new UsjVerse(node.Number, startId, node.Style);

    protected override IUsjNode CreatePara(ParaNode node, string? vid, IList<IUsjNode>? children) =>
        new UsjPara(vid, children, node.Style);

    protected override IUsjNode CreateChar(CharNode node, IList<IUsjNode>? children)
    {
        var result = new UsjChar(children, node.Style);
        foreach (var attribute in node.Attributes)
            result.ExtraProperties[attribute.Key] = JsonSerializer.SerializeToElement(attribute.Value);
        return result;
    }

    protected override IUsjNode CreateText(TextNode node) =>
        new UsjText(node.Text);

    protected override IUsjNode CreateNote(NoteNode node, IList<IUsjNode>? children) =>
        new UsjNote(node.Caller, children, node.Style);

    protected override IUsjNode CreateMilestone(MilestoneNode node) =>
        new UsjMilestone(node.StartId, node.EndId, node.Who, node.Style);

    protected override IUsjNode CreateLineBreak(LineBreakNode node) =>
        new UsjLineBreak(node.Style);

    protected override IUsjNode CreateTable(TableNode node, IList<IUsjNode>? children) =>
        new UsjTable(children, node.Style);

    protected override IUsjNode CreateRow(RowNode node, IList<IUsjNode>? children) =>
        new UsjRow(children, node.Style);

    protected override IUsjNode CreateCell(CellNode node, IList<IUsjNode>? children) =>
        new UsjCell(node.Align, children, node.Style);

    protected override IUsjNode CreateAnnotation(AnnotationNode node) =>
        new UsjChar([new UsjText(node.Text)], node.Style);
}
