using Assembly = System.Reflection.Assembly;

namespace USJ.Tests.Data;

public class UsjDataGeneratorAttribute : DataSourceGeneratorAttribute<(string, Stream)>
{
    protected override IEnumerable<Func<(string, Stream)>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(GlobalHooks.JsonFileName) &&
                assembly.GetManifestResourceStream(name) is Stream stream)
            {
                yield return () => (name, stream);
            }
        }
    }
}
