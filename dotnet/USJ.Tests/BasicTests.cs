using System.Text.Json;

namespace USJ.Tests
{
    public class BasicTests
    {
        [Test]
        [Arguments("minimal")]
        public async Task DeserializeUsjJson_FromEmbeddedResource(string resourceName)
        {
            (var fullResourceName, var stream) = GlobalHooks.LoadEmbeddedFile(resourceName);
            await Assert.That(stream).IsNotNull();
            var book = await JsonSerializer.DeserializeAsync<UsjDocument>(stream);
            await Assert.That(book).IsNotNull();
            await Assert.That(book!.Type).IsEqualTo("USJ");
            await Assert.That(book.Version).IsEqualTo("0.0.1-alpha.2");
            await Assert.That(book.Content).IsNotNull();
            await Assert.That(book.Content.Count).IsEqualTo(3);
        }

        [Test]
        public async Task DeserializeUsjJson_WithArguments()
        {
            var resourceName = "milestones";
            (var fullResourceName, var stream) = GlobalHooks.LoadEmbeddedFile(resourceName);
            TestContext.Current?.OutputWriter.WriteLine(fullResourceName);
            await Assert.That(stream).IsNotNull();
            var book = await JsonSerializer.DeserializeAsync<UsjDocument>(stream);
            await Assert.That(book).IsNotNull();
            await Assert.That(book.Content).IsNotNull();
            var para = book.Content.OfType<UsjPara>().FirstOrDefault();
            await Assert.That(para).IsNotNull();
            await Assert.That(para.Content).IsNotNull();
            var milestone = para.Content.OfType<UsjMilestone>().FirstOrDefault();
            await Assert.That(milestone).IsNotNull();
            await Assert.That(milestone.Type).IsEqualTo("ms:qt-s");
            await Assert.That(milestone.Who).IsEqualTo("Pilate");
            await Assert.That(milestone.StartId).IsEqualTo("qt_123");
        }
    }
}
