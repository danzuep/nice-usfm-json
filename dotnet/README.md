# .NET projects

Build the complete solution from the repository root:

```sh
dotnet build ./dotnet/USJ.slnx
```

The solution contains the USJ, USX, and USFM libraries together with their test projects. The USFM tests use TUnit; when running the test project directly with .NET 10, use the executable test runner if the legacy VSTest target is unavailable:

```sh
dotnet run --project ./dotnet/USFM.Tests/USFM.Tests.csproj
```