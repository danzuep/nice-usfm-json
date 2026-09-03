using USFM.Parsers;
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
}