namespace USX.Tests
{
    public class UsxDataGeneratorAttribute : DataSourceGeneratorAttribute<(string, Stream, Stream)>
    {
        protected override IEnumerable<Func<(string, Stream, Stream)>> GenerateDataSources(
            DataGeneratorMetadata dataGeneratorMetadata)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var sampleDirs = new[]
            {
                "attributes", "chapter_verse", "character", "cross_refs", "custom_attributes",
                "default_attributes", "footnote", "header", "header2", "list", "milestones",
                "minimal", "multiple_chapters", "multiple_paragraphs", "nesting", "section", "table"
            };

            foreach (var dir in sampleDirs)
            {
                var usxResourceName = $"Data.samples.{dir}.origin.xml";
                var jsonResourceName = $"Data.samples.{dir}.proposed.json";

                var usxStream = assembly.GetManifestResourceStream(usxResourceName);
                var jsonStream = assembly.GetManifestResourceStream(jsonResourceName);

                if (usxStream != null && jsonStream != null)
                {
                    yield return () => (dir, usxStream, jsonStream);
                }
            }
        }
    }

    public class StreamDataGeneratorAttribute : DataSourceGeneratorAttribute<(string, Stream)>
    {
        protected override IEnumerable<Func<(string, Stream)>> GenerateDataSources(
            DataGeneratorMetadata dataGeneratorMetadata)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith("proposed.json") &&
                    assembly.GetManifestResourceStream(name) is Stream stream)
                {
                    yield return () => (name, stream);
                }
            }
        }
    }
}
