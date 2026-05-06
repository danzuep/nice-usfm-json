using Assembly = System.Reflection.Assembly;

namespace USFM.Tests;

public class UsfmDataGeneratorAttribute : DataSourceGeneratorAttribute<(string, Stream, Stream)>
{
    internal static readonly string AssemblyDir = "USFM.Tests.Data";

    protected override IEnumerable<Func<(string, Stream, Stream)>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var sampleFolders = GetSampleFolders();

        foreach (var folderName in sampleFolders)
        {
            var resourceName = folderName.Replace("-", "_");
            var usfmResourceName = $"{AssemblyDir}.{resourceName}.origin.usfm";
            var jsonResourceName = $"{AssemblyDir}.{resourceName}.proposed.json";

            var usfmStream = assembly.GetManifestResourceStream(usfmResourceName);
            var jsonStream = assembly.GetManifestResourceStream(jsonResourceName);

            if (usfmStream != null && jsonStream != null)
            {
                yield return () => (folderName, usfmStream, jsonStream);
            }
        }
    }

    public static string FindDirectory(string folderName, string? baseDirectory = null)
    {
        var dir = new DirectoryInfo(baseDirectory ?? AppContext.BaseDirectory);

        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, folderName);
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate repository root containing '{folderName}'.");
    }

    public static IList<string> GetSampleFolders()
    {
        var samplesDir = FindDirectory("samples");

#pragma warning disable CS8619 // Nullability warning for LINQ chain - we filter out nulls
        return Directory
            .EnumerateDirectories(samplesDir)
            .Select(Path.GetFileName!)
            .Where(name => name is not null && name != "usx")
            .OrderBy(name => name)
            .ToList();
#pragma warning restore CS8619
    }
}

public class StreamDataGeneratorAttribute : DataSourceGeneratorAttribute<(string, Stream)>
{
    protected override IEnumerable<Func<(string, Stream)>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith("origin.usfm") &&
                assembly.GetManifestResourceStream(name) is Stream stream)
            {
                yield return () => (name, stream);
            }
        }
    }
}
