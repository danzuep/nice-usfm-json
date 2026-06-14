using USFM.Visitors;

namespace USFM.Tests;

public class UsfmLexerTests
{
    [Test]
    public async Task Verse()
    {
        var expected = @"\v 1 verse";
        var token = GetFirstToken(expected);
        var node = new VerseNode(token.Type.ToString(), token.Value.ToString(), token.Extra.ToString());
        await Assert.That(node?.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task WordAnnotation()
    {
        var expected = @"\w gracious|lemma=""grace"" \w*";
        var input = @$"{expected}Next";
        var token = GetFirstToken(input);
        var node = new AnnotationNode(token.Type.ToString(), token.Value.ToString(), token.Extra.ToString());
        await Assert.That(node?.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task ChapterVerse()
    {
        var expected = new string[]
        {
            @"\v 1 ",
            @"\va 3\va* ",
            @"\vp 1b\vp* ",
            "This *"
        };
        var tokens = UsfmTokenDto.Tokenize(string.Concat(expected));
        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That(tokens[i]?.ToString()).IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task SectionAnnotation()
    {
        var expected = new string[]
        {
            @"\s ",
            @"\jmp |link-id=""article-john_the_baptist"" \jmp*",
            "John the Baptist"
        };
        var tokens = UsfmTokenDto.Tokenize(string.Concat(expected));
        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That(tokens[i]?.ToString()).IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task MarkerWithAttributesAndText()
    {
        var input = "\\x - \\xo 2.23: \\xt Mrk 1.24; Luk 2.39; Jhn 1.45.\\x*and made his home in a town named Nazareth.";
        var tokenizer = new UsfmLexer(input.AsSpan());

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
    public async Task QuoteWithAttributes()
    {
        var input = "\\qt-s |sid=\"qt_123\" who=\"Pilate\" \\*“Are you the king of the Jews?\\qt-e |eid=\"qt_123\" \\*";
        var tokenizer = new UsfmLexer(input.AsSpan());

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
        var tokenizer = new UsfmLexer(input.AsSpan());
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
        var tokenizer = new UsfmLexer(input.AsSpan());
        var tokenStrings = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var tk)) tokenStrings.Add((tk.Type.ToString(), tk.Value.ToString()));

        // Ensure we don't see duplicate marker names in the concatenated output
        var concatenated = string.Concat(tokenStrings.Select(t => t.Type + ":" + t.Value + ";"));
        await Assert.That(concatenated).DoesNotContain("w\\w*");
    }

    [Test]
    public async Task MilestoneMarker()
    {
        var expected = "\\ms +\\nd 1\\ms*";
        var tokens = UsfmTokenDto.Tokenize(expected);
        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That($"{tokens[i]}").IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task MilestoneMarker2()
    {
        var input = "\\ms +\\nd 1\\ms*";
        var tokenizer = new UsfmLexer(input.AsSpan());
        var tokenStrings = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var tk)) tokenStrings.Add((tk.Type.ToString(), tk.Value.ToString()));

        await Assert.That(tokenStrings).IsNotNull();
        var ms = tokenStrings.FirstOrDefault(t => t.Type == "ms");
        await Assert.That(ms.Type).IsEqualTo("ms");
        await Assert.That(ms.Value).Contains("+\\nd 1");
    }

    [Test]
    public async Task AdjacentInlineMarkers()
    {
        var input = "\\v 1 start \\w one|lemma=\"one\" \\w*\\w two|lemma=\"two\" \\w* end";
        var tokenizer = new UsfmLexer(input.AsSpan());
        var tokenStrings = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var tk)) tokenStrings.Add((tk.Type.ToString(), tk.Value.ToString()));

        // Ensure each inline word marker appears once and closers are not duplicated
        var wCount = tokenStrings.Count(t => t.Type == "w");
        await Assert.That(wCount).IsEqualTo(2);
        var concatenated = string.Concat(tokenStrings.Select(t => t.Type + ":" + t.Value + ";"));
        await Assert.That(concatenated).DoesNotContain("w\\w*");
    }

    private static UsfmToken GetFirstToken(string input)
    {
        var tokenizer = new UsfmLexer(input.AsSpan());
        _ = tokenizer.TryMoveNext(out var usfmToken);
        return usfmToken;
    }
}
