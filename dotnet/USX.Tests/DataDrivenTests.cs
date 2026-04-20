using System.Text.Json;
using System.Text.Json.Serialization;
using Bible.Usx.Services;
using USJ;
using USX.Models;

namespace USX.Tests
{
    public class DataDrivenTests
    {
        [Test]
        [StreamDataGenerator]
        public async Task DeserializeUsjJson_WithCustomDataGenerator(string name, Stream stream)
        {
            TestContext.Current?.OutputWriter.WriteLine(name);
            await Assert.That(stream).IsNotNull();
            var book = await JsonSerializer.DeserializeAsync<UsjBook>(stream);
            await Assert.That(book).IsNotNull();
        }

        [Test]
        [UsxDataGenerator]
        public async Task ConvertUsxToUsj_WithUsxDataGenerator(string name, Stream usxStream, Stream expectedJsonStream)
        {
            TestContext.Current?.OutputWriter.WriteLine(name);
            await Assert.That(usxStream).IsNotNull();
            await Assert.That(expectedJsonStream).IsNotNull();

            var converter = new UsxToUsjConverter();
            var actualBook = await converter.ConvertUsxStreamToUsjBookAsync(usxStream);

            await Assert.That(actualBook).IsNotNull();

            // Serialize actual result to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var actualJson = JsonSerializer.Serialize(actualBook, options);

            // Deserialize expected JSON
            expectedJsonStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(expectedJsonStream);
            var expectedJson = await reader.ReadToEndAsync();

            // Compare JSON structures
            var expectedDoc = JsonSerializer.Deserialize<UsjDocument>(expectedJson);
            var actualDoc = JsonSerializer.Deserialize<UsjDocument>(actualJson);

            await Assert.That(actualDoc).IsEquivalentTo(expectedDoc);
        }
    }
}
