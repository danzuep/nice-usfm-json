namespace USFM.Tests;

public class UsfmTokenizerTests
{
    [Test]
    public async Task MarkerWithAttributesAndText()
    {
        var input = "\\x - \\xo 2.23: \\xt Mrk 1.24; Luk 2.39; Jhn 1.45.\\x*and made his home in a town named Nazareth.";
        var tokenizer = new UsfmTokenizer(input.AsSpan());

        // collect pure strings from tokenizer (do not preserve ref structs across awaits)
        var tokenStrings = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var tk)) tokenStrings.Add((tk.Type.ToString(), tk.Value.ToString()));

        await Assert.That(tokenStrings).IsNotNull();
        // find an \x marker token and ensure nested markers like \xo appear in the stream
        var x = tokenStrings.FirstOrDefault(t => t.Type == "x");
        await Assert.That(x.Type).IsEqualTo("x");
        // ensure there is an \xo token following somewhere
        var hasXo = tokenStrings.Any(t => t.Type == "xo");
        await Assert.That(hasXo).IsTrue();
    }

    [Test]
    public async Task QtWithAttributes()
    {
        var input = "\\qt-s |sid=\"qt_123\" who=\"Pilate\" \\*“Are you the king of the Jews?\\qt-e |eid=\"qt_123\" \\*";
        var tokenizer = new UsfmTokenizer(input.AsSpan());

        var tokenStrings = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var tk)) tokenStrings.Add((tk.Type.ToString(), tk.Value.ToString()));

        await Assert.That(tokenStrings).IsNotNull();
        var qt = tokenStrings.FirstOrDefault(t => t.Type.StartsWith("qt-s"));
        await Assert.That(qt.Type).Contains("qt-s");
        await Assert.That(qt.Value).Contains("sid=\"qt_123\"");
    }

    [Test]
    public async Task SimpleMarkerAndText()
    {
        var input = "\\v 2 the second verse \\w gracious|lemma=\"grace\" \\w*";
        var tokenizer = new UsfmTokenizer(input.AsSpan());
        var tokenStrings = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var tk)) tokenStrings.Add((tk.Type.ToString(), tk.Value.ToString()));

        await Assert.That(tokenStrings).IsNotNull();
        var v = tokenStrings.FirstOrDefault(t => t.Type == "v");
        var w = tokenStrings.FirstOrDefault(t => t.Type == "w");
        await Assert.That(v.Type).IsEqualTo("v");
        await Assert.That(v.Value).Contains("2 the second verse");
        await Assert.That(w.Type).IsEqualTo("w");
        await Assert.That(w.Value).Contains("gracious|lemma=");
    }

    [Test]
    public async Task MarkersDoNotDuplicateClosers()
    {
        var input = "\\v 2 the second verse \\w gracious|lemma=\"grace\" \\w*";
        var tokenizer = new UsfmTokenizer(input.AsSpan());
        var tokenStrings = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var tk)) tokenStrings.Add((tk.Type.ToString(), tk.Value.ToString()));

        // Ensure we don't see duplicate marker names in the concatenated output
        var concatenated = string.Concat(tokenStrings.Select(t => t.Type + ":" + t.Value + ";"));
        await Assert.That(concatenated).DoesNotContain("w\\w*");
    }
}
