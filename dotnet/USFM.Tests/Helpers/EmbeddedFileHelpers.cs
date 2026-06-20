using System.Reflection;

namespace USFM.Tests.Helpers
{
    internal class EmbeddedFileHelpers
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static readonly string UsfmFileName = "origin.usfm";
        internal static readonly string AssemblyDir = "USFM.Tests.Data";

        public static (string, Stream?) LoadEmbeddedFile(string resourceName)
        {
            string fullResourceName = $"{AssemblyDir}.{resourceName}.{UsfmFileName}";
            return (fullResourceName, Assembly.GetManifestResourceStream(fullResourceName));
        }
    }
}
