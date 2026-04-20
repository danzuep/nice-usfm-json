using System.Diagnostics.CodeAnalysis;
using System.Reflection;

[assembly: ExcludeFromCodeCoverage]

namespace USJ.Tests
{
    public class GlobalHooks
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static Assembly Assembly;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static readonly string JsonFileName = "proposed.json";
        internal static readonly string AssemblyDir = "USJ.Tests.Data"; // Path format: Namespace.Folder.File

        [Before(Class)]
        public static Task BeforeClass(ClassHookContext context)
        {
            // Runs once before all tests in this class
            return Task.CompletedTask;
        }

        [After(Class)]
        public static Task AfterClass(ClassHookContext context)
        {
            // Runs once after all tests in this class
            return Task.CompletedTask;
        }

        [Before(Test)]
        public Task BeforeTest(TestContext context)
        {
            // Runs before each test in this class
            return Task.CompletedTask;
        }

        [After(Test)]
        public Task AfterTest(TestContext context)
        {
            // Runs after each test in this class
            return Task.CompletedTask;
        }

        [Before(TestSession)]
        public static Task BeforeTestSession(TestSessionContext context)
        {
            Assembly = Assembly.GetExecutingAssembly();
            // Runs once before all tests - e.g. start a test container, seed a database
            return Task.CompletedTask;
        }

        [After(TestSession)]
        public static Task AfterTestSession(TestSessionContext context)
        {
            // Runs once after all tests - e.g. stop containers, clean up resources
            return Task.CompletedTask;
        }

        public static (string, Stream?) LoadEmbeddedFile(string resourceName)
        {
            string fullResourceName = $"{AssemblyDir}.{resourceName}.{JsonFileName}";
            return (fullResourceName, Assembly.GetManifestResourceStream(fullResourceName));
        }
    }
}
