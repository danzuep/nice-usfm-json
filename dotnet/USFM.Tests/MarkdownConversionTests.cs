namespace USFM.Tests
{
    public class MarkdownConversionTests
    {
        [Test]
        public async Task ConvertMinimalUsfmToMarkdown()
        {
            // Load the minimal USFM file using the same approach as BasicTests
            var (_, stream) = LoadEmbeddedFile("minimal");

            await Assert.That(stream).IsNotNull();

            var converter = new UsfmConverter();
            var markdown = await converter.ConvertUsfmToMarkdownAsync(stream);

            // Verify the markdown output contains expected elements
            await Assert.That(markdown).Contains("# ");
            await Assert.That(markdown).Contains("GEN");
            await Assert.That(markdown).Contains("Chapter 1");
            await Assert.That(markdown).Contains("**1**");
            await Assert.That(markdown).Contains("**2**");
            await Assert.That(markdown).Contains("verse one");
            await Assert.That(markdown).Contains("verse two");
        }

        private static (string, Stream?) LoadEmbeddedFile(string resourceName)
        {
            var assembly = typeof(BasicTests).Assembly;
            var fullResourceName = $"USFM.Tests.Data.{resourceName}.origin.usfm";
            return (fullResourceName, assembly.GetManifestResourceStream(fullResourceName));
        }
    }
}