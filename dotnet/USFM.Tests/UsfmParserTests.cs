using System.Diagnostics;
using System.Reflection.PortableExecutable;
using USFM.Visitors;

namespace USFM.Tests
{
    public class UsfmParserTests
    {
        [Explicit("Use for manual verification")]
        [Test]
        public async Task ConvertUsfm_ManualVerification()
        {
            // Load the minimal USFM file using the same approach as BasicTests
            var name = "chapter_verse";
            var (_, stream) = LoadEmbeddedFile(name);

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

        [Test]
        [StreamDataGenerator]
        public async Task ConvertUsfm_WithUsfmDataGenerator(string name, Stream usfmStream)
        {
            TestContext.Current?.OutputWriter.WriteLine(name);
            await Assert.That(usfmStream).IsNotNull();

            using var reader = new StreamReader(usfmStream);
            var nodes = await UsfmConverter.ConvertUsfmAsync(reader);
            usfmStream.Seek(0, SeekOrigin.Begin);

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
#if DEBUG
            var path = Path.Combine("..", "..", $"usfm_{name}.txt");
            File.Delete(path);
            foreach (var node in nodes)
            {
                await File.AppendAllTextAsync(path, $"{node}\n");
            }
            Debug.WriteLine($"USFM written to: {path}");
#endif
        }

        private static (string, Stream?) LoadEmbeddedFile(string resourceName)
        {
            var assembly = typeof(BasicTests).Assembly;
            var fullResourceName = $"USFM.Tests.Data.{resourceName}.origin.usfm";
            return (fullResourceName, assembly.GetManifestResourceStream(fullResourceName));
        }
    }
}