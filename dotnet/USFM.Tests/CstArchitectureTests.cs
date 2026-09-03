using USFM.Parsers;
using USFM.Ast;
using USFM.Visitors;

namespace USFM.Tests;

public class CstArchitectureTests
{
    [Test]
    public async Task CstRetainsSourceSlicesAndDiagnostics()
    {
        const string source = "\\w word|lemma=\"x\" src=\"G1234\"\\w*";
        var parser = new UsfmCstParser(source.AsMemory());
        var root = parser.Parse();
        var diagnostics = parser.Diagnostics;
        var marker = await Assert.That(root.Children[0]).IsTypeOf<CstMarkerNode>();

        await Assert.That(marker!.MarkerName.ToString()).IsEqualTo("w");
        await Assert.That(marker.Attributes).Count().IsEqualTo(2);
        await Assert.That(source.AsSpan(marker.Span.Start, marker.Span.Length).ToString()).IsEqualTo(source);
        await Assert.That(diagnostics).Count().IsEqualTo(0);
    }

    [Test]
    public async Task LowererPreservesAttributesAndVerseRanges()
    {
        const string source = "\\v 1-3 text \\w word|lemma=\"x\"\\w*";
        var nodes = CstToAstLowerer.Parse(source.AsMemory(), out var diagnostics);
        var verse = await Assert.That(nodes[0]).IsTypeOf<VerseNode>();
        var character = await Assert.That(nodes.OfType<CharNode>().Single()).IsTypeOf<CharNode>();

        await Assert.That(diagnostics).Count().IsEqualTo(0);
        await Assert.That(verse!.StartVerse).IsEqualTo("1");
        await Assert.That(verse.EndVerse).IsEqualTo("3");
        await Assert.That(character!.Attributes["lemma"]).IsEqualTo("x");
    }

    [Test]
    public async Task CstResultReportsStableRecoveryDiagnostics()
    {
        var parsed = Usfm.ParseCstResult("\\w unfinished".AsMemory());

        await Assert.That(parsed.Diagnostics).Count().IsEqualTo(1);
        await Assert.That(parsed.Diagnostics[0].Code).IsEqualTo("USFM002");
        await Assert.That(parsed.Diagnostics[0].Span.Start).IsEqualTo(0);
        await Assert.That(parsed.Cst.Span.Length).IsEqualTo(parsed.Source.Length);
    }

    [Test]
    public async Task CstPreservesDuplicateAttributeOrder()
    {
        const string source = "\\w word|x=\"one\" x=\"two\"\\w*";
        var parser = new UsfmCstParser(source.AsMemory());
        var root = parser.Parse();
        var marker = await Assert.That(root.Children[0]).IsTypeOf<CstMarkerNode>();

        await Assert.That(marker!.Attributes).Count().IsEqualTo(2);
        await Assert.That(marker.Attributes[0].Value.ToString()).IsEqualTo("one");
        await Assert.That(marker.Attributes[1].Value.ToString()).IsEqualTo("two");
    }

    [Test]
    public async Task ParseAstBuildsSourceMapForCstNodes()
    {
        var parsed = Usfm.ParseAst("\\c 1\n\\p\n\\v 1 text".AsMemory());

        await Assert.That(parsed.SourceMap.Spans.Count).IsGreaterThan(1);
        await Assert.That(parsed.SourceMap.TryGetSpan(0, out var rootSpan)).IsTrue();
        await Assert.That(rootSpan.Length).IsEqualTo(parsed.Source.Length);
    }

    [Test]
    public async Task LowererRetainsDuplicateSemanticAttributes()
    {
        var nodes = CstToAstLowerer.Parse("\\w word|x=\"one\" x=\"two\"\\w*".AsMemory(), out _);
        var character = await Assert.That(nodes.OfType<CharNode>().Single()).IsTypeOf<CharNode>();

        await Assert.That(character!.Attributes).Count().IsEqualTo(2);
        await Assert.That(character.Attributes[0].Key).IsEqualTo("x");
        await Assert.That(character.Attributes[1].Value).IsEqualTo("two");
        await Assert.That(character.Attributes[0].Span.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task LowererKeepsMilestoneStartAndEndNodes()
    {
        const string source = "\\qt-s |sid=\"q1\" \\*quoted\\qt-e |eid=\"q1\" \\*";
        var nodes = CstToAstLowerer.Parse(source.AsMemory(), out var diagnostics);
        var milestones = nodes.OfType<MilestoneNode>().ToArray();

        await Assert.That(diagnostics).Count().IsEqualTo(0);
        await Assert.That(milestones).Count().IsEqualTo(2);
        await Assert.That(milestones[0].StartId).IsEqualTo("q1");
        await Assert.That(milestones[1].EndId).IsEqualTo("q1");
    }
}