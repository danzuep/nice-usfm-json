using USFM.Lexers;
using USFM.Parsers;
using USFM.Tests.Helpers;
using USFM.Visitors;

namespace USFM.Tests
{
    public class UsfmParserTests
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
            await Assert.That(text2.Text).IsEqualTo("After");
        }

        private static T ParseSingle<T>(string usfm) where T : IUsfmNode
        {
            //var node = ParseSingle<VerseNode>(input);
            var nodes = UsfmParserStrategy.Parse(usfm);
            var node = (T)nodes.Single();
            return node;
        }

        [Skip("Not ready to be run yet")]
        [Explicit("Use for manual verification")]
        [Test]
        public async Task ConvertUsfm_ManualVerification()
        {
            // Load the minimal USFM file using the same approach as BasicTests
            var name = "chapter_verse";
            var (_, stream) = EmbeddedFileHelpers.LoadEmbeddedFile(name);

            await Assert.That(stream).IsNotNull();

            using var reader = new StreamReader(stream);
            var nodes = await UsfmConverter.ConvertUsfmAsync(reader);
            stream.Seek(0, SeekOrigin.Begin);

            await Assert.That(nodes).IsNotNull();
            await Assert.That(nodes.Count).IsGreaterThan(0);

            var index = 0;
            string? expected;
            while ((expected = await reader.ReadLineAsync()) != null)
            {
                var actualNode = nodes[index];
                await Assert.That(index).IsLessThan(nodes.Count);
                var actual = $"{nodes[index]}";
                while (actual.Length < expected.Length && ++index < nodes.Count)
                {
                    var nextNode = nodes[index];
                    actual = $"{actual}{nextNode}";
                }
                if (actual != expected)
                {
                    TestContext.Current?.OutputWriter.WriteLine($"Expected: {expected}");
                    TestContext.Current?.OutputWriter.WriteLine($"Actual:   {actual}");
                    await Assert.That(actual).IsEqualTo(expected);
                }
                index++;
            }

// #if DEBUG
//             var path = Path.Combine("..", "..", $"usfm_{name}.txt");
//             File.Delete(path);
//             foreach (var node in nodes)
//             {
//                 TestContext.Current?.OutputWriter.WriteLine($"{node}");
//                 await File.AppendAllTextAsync(path, $"{node}");
//             }
//             Debug.WriteLine($"USFM written to: {path}");
// #endif
        }
    }
}