using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        var converter = new UsfmConverter();
        var actualDocument = await converter.ConvertUsfmToUsjAsync(usfmStream);

        await Assert.That(actualDocument).IsNotNull();

        // Serialize actual result to JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var actualJson = JsonSerializer.Serialize(actualDocument, options);

        // Deserialize expected JSON
        expectedJsonStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(expectedJsonStream);
        var expectedJson = await reader.ReadToEndAsync();

        // Compare JSON structures (allowing different attribute order)
        var expectedDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(expectedJson);
        var actualDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(actualJson);

#if DEBUG
        var path1 = Path.Combine("..", "..", $"{name}_expected.json");
        await File.WriteAllTextAsync(path1, expectedJson);
        Debug.WriteLine($"Serialized JSON written to: {path1}");
        var path2 = Path.Combine("..", "..", $"{name}_actual.json");
        await File.WriteAllTextAsync(path2, actualJson);
        Debug.WriteLine($"Serialized JSON written to: {path2}");
#endif

        await Assert.That(expectedDoc).IsNotNull();
        await Assert.That(actualDoc).IsEquivalentTo(expectedDoc);
    }
}