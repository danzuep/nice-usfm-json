using System.Diagnostics;

namespace USFM.Tests
{
    public class UsfmParserTests
    {
        [Explicit("Use for manual verification")]
        [Test]
        public async Task ConvertUsfm_ManualVerification()
        {
            // Load the minimal USFM file using the same approach as BasicTests
            var name = "milestones";
            var (_, stream) = LoadEmbeddedFile(name);

            await Assert.That(stream).IsNotNull();

            var converter = new UsfmConverter();
            var nodes = await converter.ConvertUsfmAsync(stream);

            await Assert.That(nodes).IsNotNull();

            foreach (var node in nodes)
            {
                TestContext.Current?.OutputWriter.WriteLine($"{node}");
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

        [Test]
        [StreamDataGenerator]
        public async Task ConvertUsfm_WithUsfmDataGenerator(string name, Stream usfmStream)
        {
            TestContext.Current?.OutputWriter.WriteLine(name);
            await Assert.That(usfmStream).IsNotNull();

            var converter = new UsfmConverter();
            var nodes = await converter.ConvertUsfmAsync(usfmStream);

            await Assert.That(nodes).IsNotNull();

            foreach (var node in nodes)
            {
                TestContext.Current?.OutputWriter.WriteLine($"{node}");
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