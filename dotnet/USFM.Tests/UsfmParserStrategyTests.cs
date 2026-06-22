using USFM.Lexers;
using USFM.Parsers;
using USFM.Visitors;

namespace USFM.Tests
{
    public class UsfmParserStrategyTests
    {
        [Test]
        public async Task Verse()
        {
            var input = @"\v 1 verse";
            var nodes = UsfmParserStrategy.Parse(input);
            await Assert.That(nodes.Count).IsEqualTo(2);
            var verse = (VerseNode)nodes[0];
            var text = (TextNode)nodes[1];
            await Assert.That(verse.Style).IsEqualTo("v");
            await Assert.That(verse.Number).IsEqualTo("1");
            await Assert.That(text.Text).IsEqualTo("verse");
        }

        [Test]
        public async Task WordAnnotation()
        {
            var expected = @"\w gracious|lemma=""grace"" \w*";
            var input = @$"Before{expected}After";
            var nodes = UsfmParserStrategy.Parse(input);
            await Assert.That(nodes.Count).IsEqualTo(3);
            var text1 = (TextNode)nodes[0];
            var annotation = (AnnotationNode)nodes[1];
            var text2 = (TextNode)nodes[2];
            await Assert.That(text1.Text).IsEqualTo("Before");
            await Assert.That(annotation.Style).IsEqualTo("w");
            await Assert.That(annotation.Text).IsEqualTo("gracious|lemma=\"grace\"");
            await Assert.That(annotation.End).IsEqualTo(@"\w*");
            await Assert.That(text2.Text).IsEqualTo("After");
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
            var nodes = UsfmParserStrategy.Parse(string.Concat(expected));

            for (int i = 0; i < nodes.Count; i++)
            {
                await Assert.That(nodes[i].ToString()).IsEqualTo(expected[i]);
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
            var tokens = UsfmLexerTokenDto.Tokenize(expected);

            for (int i = 0; i < tokens.Count; i++)
            {
                await Assert.That(tokens[i].Raw).IsEqualTo(expected[i]);
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
            var tokens = UsfmLexerTokenDto.Tokenize(expected);

            for (int i = 0; i < tokens.Count; i++)
            {
                await Assert.That(tokens[i].Raw).IsEqualTo(expected[i]);
            }
        }

        [Test]
        public async Task QuoteEnd()
        {
            var expected = @"\qt-e |eid=""qt_123"" \*";
            var token = UsfmLexerTokenDto.Tokenize(expected).Single();

            await Assert.That(token.Raw).IsEqualTo(expected);
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
            var tokens = UsfmLexerTokenDto.Tokenize(expected);

            for (int i = 0; i < tokens.Count; i++)
            {
                await Assert.That(tokens[i].Raw).IsEqualTo(expected[i]);
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
            var tokens = UsfmLexerTokenDto.Tokenize(expected);

            for (int i = 0; i < tokens.Count; i++)
            {
                await Assert.That(tokens[i].Raw).IsEqualTo(expected[i]);
            }
            await Assert.That(tokens[1].Segments[1]).IsEqualTo("gracious|lemma=\"grace\" ");
        }

        [Test]
        public async Task MilestoneMarker()
        {
            var expected = @"\ms +\nd 1\ms*";
            var token = UsfmLexerTokenDto.Tokenize(expected).Single();

            await Assert.That(token.Raw).IsEqualTo(expected);
            await Assert.That(token.Segments[0]).IsEqualTo(@"\ms ");
            await Assert.That(token.Segments[1]).IsEqualTo(@"+\nd 1");
            await Assert.That(token.Segments[2]).IsEqualTo(@"\ms*");
        }

        [Test]
        public async Task AdjacentInlineMarkers()
        {
            var expected = new string[]
            {
            @"\v 1 start ",
            @"\w one|lemma=""one"" \w*",
            @"\w two|lemma=""two"" \w* ",
            "end"
            };
            var tokens = UsfmLexerTokenDto.Tokenize(expected);

            for (int i = 0; i < tokens.Count; i++)
            {
                await Assert.That(tokens[i].Raw).IsEqualTo(expected[i]);
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
            var token = UsfmLexerTokenDto.Tokenize(expected).First();

            await Assert.That(token.Segments[1]).IsEqualTo(expected[1]);
            await Assert.That(token.Segments[2]).IsEqualTo(expected[2]);
        }

        [Test]
        public async Task ChapterAttributes()
        {
            var expected = new string[]
            {
            @"\c 1",
            @"\cl Matthew",
            @"\ca 2\ca*",
            @"\cp M"
            };
            var tokens = UsfmLexerTokenDto.Tokenize(expected);

            for (int i = 0; i < tokens.Count; i++)
            {
                await Assert.That(tokens[i].Raw).IsEqualTo(expected[i]);
            }
        }
    }
}