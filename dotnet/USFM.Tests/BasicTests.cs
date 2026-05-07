using System.Reflection;
using USJ;

namespace USFM.Tests
{
    public class BasicTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static Assembly Assembly;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static readonly string UsfmFileName = "origin.usfm";
        internal static readonly string AssemblyDir = "USFM.Tests.Data";

        static BasicTests()
        {
            Assembly = Assembly.GetExecutingAssembly();
        }

        [Test]
        [Arguments("minimal")]
        public async Task DeserializeUsjJson_FromEmbeddedResource(string resourceName)
        {
            (var fullResourceName, var usfmStream) = LoadEmbeddedFile(resourceName);
            await Assert.That(usfmStream).IsNotNull();
            var converter = new UsfmToUsjConverter();
            var actualDocument = await converter.ConvertUsfmToUsjAsync(usfmStream);
            await Assert.That(actualDocument).IsNotNull();
            await Assert.That(actualDocument.Type).IsEqualTo(UsjDocument.UsjType);
            await Assert.That(actualDocument.Version).IsEqualTo(UsjDocument.UsjVersion);
            await Assert.That(actualDocument.Content).IsNotNull();
            await Assert.That(actualDocument.Content.Count).IsEqualTo(4);
        }

        public static (string, Stream?) LoadEmbeddedFile(string resourceName)
        {
            string fullResourceName = $"{AssemblyDir}.{resourceName}.{UsfmFileName}";
            return (fullResourceName, Assembly.GetManifestResourceStream(fullResourceName));
        }
    }
}
