using USFM.Visitors;

namespace USFM.Tests;

public class UsfmLexerTests
{
    [Test]
    public async Task Verse()
    {
        var expected = @"\v 1 verse";
        var token = UsfmTokenDto.Tokenize(expected).Single();
        var node = new VerseNode(token.Style, token.Value, token.Extra);
        await Assert.That(node?.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task WordAnnotation()
    {
        var expected = @"\w gracious|lemma=""grace"" \w*";
        var input = @$"{expected}Next";
        var token = UsfmTokenDto.Tokenize(expected).First();
        var node = new AnnotationNode(token.Style, token.Value, token.Extra);
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
        var expected = new string[]
        {
            @"\x - \xo 2.23: \xt Mrk 1.24; \xt Luk 2.39; \xt Jhn 1.45.\x*",
            "and made his home in a town named Nazareth."
        };
        var tokens = UsfmTokenDto.Tokenize(string.Concat(expected));

        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That(tokens[i]?.ToString()).IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task QuoteEnd()
    {
        var expected = @"\qt-e |eid=""qt_123"" \*";
        var token = UsfmTokenDto.Tokenize(expected).Single();

        await Assert.That(token.ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task QuoteWithAttributes()
    {
        var expected = new string[]
        {
            @"\qt-s |sid=""qt_123"" who=""Pilate"" \*",
            "Are you the king of the Jews?",
            @"\qt-e |eid=""qt_123"" \*"
        };
        var tokens = UsfmTokenDto.Tokenize(string.Concat(expected));

        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That(tokens[i]?.ToString()).IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task MarkerAndText()
    {
        var expected = new string[]
        {
            @"\v 2 the second verse ",
            @"\w gracious|lemma=""grace"" \w*"
        };
        var tokens = UsfmTokenDto.Tokenize(string.Concat(expected));

        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That(tokens[i]?.ToString()).IsEqualTo(expected[i]);
        }
        await Assert.That(tokens[1].Value).IsEqualTo("gracious|lemma=\"grace\" ");
    }

    [Test]
    public async Task MilestoneMarker()
    {
        var expected = "\\ms +\\nd 1\\ms*";
        var token = UsfmTokenDto.Tokenize(expected).Single();

        await Assert.That($"{token}").IsEqualTo(expected);
        await Assert.That(token.Style).IsEqualTo("ms");
        await Assert.That(token.Value).IsEqualTo("+\\nd 1");
        await Assert.That(token.Extra).IsEqualTo("\\ms*");
    }

    [Test]
    public async Task AdjacentInlineMarkers()
    {
        var expected = new string[]
        {
            "\\v 1 start ",
            "\\w one|lemma=\"one\" \\w*",
            "\\w two|lemma=\"two\" \\w* ",
            "end"
        };
        var tokens = UsfmTokenDto.Tokenize(string.Concat(expected));

        for (int i = 0; i < tokens.Count; i++)
        {
            await Assert.That(tokens[i]?.ToString()).IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task InlineWord()
    {
        var expected = new string[]
        {
            @"\w ",
            @"two|lemma=""two"" ",
            @"\w* ",
            "end"
        };
        var token = UsfmTokenDto.Tokenize(string.Concat(expected)).First();

        await Assert.That(token.Value).IsEqualTo(expected[1]);
        await Assert.That(token.Extra).IsEqualTo(expected[2]);
    }
}
