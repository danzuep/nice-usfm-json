namespace USJ.Tests.Data
{
    public class StreamDataGeneratorAttribute : DataSourceGeneratorAttribute<(string, Stream)>
    {
        protected override IEnumerable<Func<(string, Stream)>> GenerateDataSources(
            DataGeneratorMetadata dataGeneratorMetadata)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(GlobalHooks.JsonFileName) ||
                    assembly.GetManifestResourceStream(name) is not Stream stream)
                {
                    continue;
                }
                yield return () => (name, stream);
            }
        }
    }
}
