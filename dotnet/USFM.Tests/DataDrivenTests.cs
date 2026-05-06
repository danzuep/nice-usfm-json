using System.Text.Json;
using System.Text.Json.Serialization;
using Bible.Usfm.Services;

namespace USFM.Tests;

public class DataDrivenTests
{
    [Test]
    [UsfmDataGenerator]
    public async Task ConvertUsfmToUsj_WithUsfmDataGenerator(string name, Stream usfmStream, Stream expectedJsonStream)
    {
        TestContext.Current?.OutputWriter.WriteLine(name);
        await Assert.That(usfmStream).IsNotNull();
        await Assert.That(expectedJsonStream).IsNotNull();

        var converter = new UsfmToUsjConverter();
        var actualDocument = await converter.ConvertUsfmToUsjAsync(usfmStream);

        await Assert.That(actualDocument).IsNotNull();

        // Serialize actual result to JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var actualJson = JsonSerializer.Serialize(actualDocument, options);

        // Deserialize expected JSON
        expectedJsonStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(expectedJsonStream);
        var expectedJson = await reader.ReadToEndAsync();

        // Compare JSON structures (allowing different attribute order)
        var expectedDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(expectedJson);
        var actualDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(actualJson);

        await Assert.That(expectedDoc).IsNotNull();
        await Assert.That(actualDoc).IsEquivalentTo(expectedDoc);
    }
}