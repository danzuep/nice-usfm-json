using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using USJ.Tests.Data;

namespace USJ.Tests
{
    public class DataDrivenTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static Assembly Assembly;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        private static readonly InMemoryDb _db = new();

        static DataDrivenTests()
        {
            Assembly = Assembly.GetExecutingAssembly();
            _db.InitializeAsync().GetAwaiter().GetResult();
        }

        [Test]
        [UsjDataGenerator]
        public async Task DeserializeUsjJson_WithCustomDataGenerator(string name, Stream stream)
        {
            TestContext.Current?.OutputWriter.WriteLine(name);
            await Assert.That(stream).IsNotNull();
            var book = await JsonSerializer.DeserializeAsync<UsjDocument>(stream);
            await Assert.That(book).IsNotNull();
        }

        [Explicit("This test is for manual validation.")]
        [Category("Manual")]
        [Test]
        [MethodDataSource(nameof(EmbeddedFiles))]
        public async Task DeserializeUsjJson_WithMethodDataSource(string name, Stream? stream)
        {
            TestContext.Current?.OutputWriter.WriteLine(name);
            await Assert.That(stream).IsNotNull();
            // The converter is automatically used because IUsjNode has [JsonConverter(typeof(UsjNodeConverter))]
            var book = await JsonSerializer.DeserializeAsync<UsjDocument>(stream);
            await Assert.That(book).IsNotNull();

            // Store expected JSON in database
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var expectedJson = await reader.ReadToEndAsync();
            await _db.SetAsync($"expected_{name}", expectedJson);

            // Serialize actual result
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var actualJson = JsonSerializer.Serialize(book, options);
            await _db.SetAsync($"actual_{name}", actualJson);

#if DEBUG
            var path1 = Path.Combine("..", "..", $"expected_{name}");
            await File.WriteAllTextAsync(path1, expectedJson);
            Debug.WriteLine($"Serialized JSON written to: {path1}");
            var path2 = Path.Combine("..", "..", $"actual_{name}");
            await File.WriteAllTextAsync(path2, actualJson);
            Debug.WriteLine($"Serialized JSON written to: {path2}");
#endif

            // Retrieve from database and compare as dictionaries to allow different attribute order
            var storedExpectedJson = await _db.GetAsync($"expected_{name}");
            var storedActualJson = await _db.GetAsync($"actual_{name}");

            await Assert.That(storedExpectedJson).IsNotNull();
            await Assert.That(storedActualJson).IsNotNull();

            var expected = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(storedExpectedJson);
            var actual = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(storedActualJson);

            await Assert.That(expected).IsNotNull();
            await Assert.That(actual).IsEquivalentTo(expected);
        }

        public IEnumerable<(string, Stream?)> EmbeddedFiles()
        {
            yield return LoadEmbeddedFile("attributes");
            yield return LoadEmbeddedFile("chapter_verse");
            yield return LoadEmbeddedFile("character");
            yield return LoadEmbeddedFile("cross_refs");
            yield return LoadEmbeddedFile("custom_attributes");
            yield return LoadEmbeddedFile("default_attributes");
            yield return LoadEmbeddedFile("footnote");
            yield return LoadEmbeddedFile("header");
            yield return LoadEmbeddedFile("header2");
            yield return LoadEmbeddedFile("list");
            yield return LoadEmbeddedFile("milestones");
            yield return LoadEmbeddedFile("minimal");
            yield return LoadEmbeddedFile("multiple_chapters");
            yield return LoadEmbeddedFile("multiple_paragraphs");
            yield return LoadEmbeddedFile("nesting");
            yield return LoadEmbeddedFile("section");
            yield return LoadEmbeddedFile("table");
        }

        internal static (string, Stream?) LoadEmbeddedFile(string resourceName)
        {
            string fullResourceName = $"{GlobalHooks.AssemblyDir}.{resourceName}.{GlobalHooks.JsonFileName}";
            return (fullResourceName, Assembly.GetManifestResourceStream(fullResourceName));
        }
    }
}
