# USFM.Tests

## Run tests

- Build and run all tests:

```sh
dotnet build ./dotnet/USFM.Tests/USFM.Tests.csproj
dotnet run --project ./dotnet/USFM.Tests/USFM.Tests.csproj --no-build -- --disable-logo
```

- List tests:

```sh
dotnet run --project dotnet/USFM.Tests/USFM.Tests.csproj -- --list-tests --diagnostic --diagnostic-verbosity Trace
```

- Run a subset by name filter:

```sh
dotnet run --project dotnet/USFM.Tests/USFM.Tests.csproj -- --filter-uid USFM.Tests.UsfmLexerTests.*
```
