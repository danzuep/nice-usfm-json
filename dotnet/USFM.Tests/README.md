## Running tests

- List tests

```sh
dotnet run --project dotnet/USFM.Tests/USFM.Tests.csproj -- --list-tests --diagnostic --diagnostic-verbosity Trace
```

- Run a subset by name filter:

```sh
dotnet run --project dotnet/USFM.Tests/USFM.Tests.csproj -- --filter-uid USFM.Tests.UsfmLexerTests.*
```
