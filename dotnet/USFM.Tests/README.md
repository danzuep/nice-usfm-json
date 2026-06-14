## Running tests

- **Note:** The repository previously enabled the legacy Microsoft Testing Platform (VSTest) MSBuild runner via `UseMicrosoftTestingPlatformRunner`. That runner is incompatible with the .NET 10 SDK. We have disabled it in `dotnet/Directory.Build.props` so `dotnet test` uses the new test experience.

- Run all .NET tests:

```bash
dotnet test dotnet/USFM.Tests/USFM.Tests.csproj
```

- List tests (works now that the legacy runner is disabled):

```bash
dotnet test dotnet/USFM.Tests/USFM.Tests.csproj --list-tests
```

- Run a subset by name filter:

```bash
dotnet test dotnet/USFM.Tests/USFM.Tests.csproj -- --filter "Name~ConvertUsfm_WithUsfmDataGenerator"
```

- If you prefer the repository's internal test runner (used by CI or local tooling), continue using that; it runs the same test assemblies and reports the same results.
