using USFM.Lexers;
using USFM.Tests.Helpers;

namespace USFM.Tests;

public class UsfmLexerTests
{
    [Test]
    [StreamDataGenerator]
    public async Task ConvertUsfm_WithUsfmDataGenerator(string name, Stream usfmStream)
    {
        TestContext.Current?.OutputWriter.WriteLine(name);
        await Assert.That(usfmStream).IsNotNull();

        using var reader = new StreamReader(usfmStream);
        var tokens = await UsfmLexerDto.TokenizeAsync(reader);
        usfmStream.Seek(0, SeekOrigin.Begin);

        await Assert.That(tokens).IsNotNull();
        await Assert.That(tokens.Count).IsGreaterThan(0);

        var index = 0;
        string? expected;
        while ((expected = await reader.ReadLineAsync()) != null)
        {
            TestContext.Current?.OutputWriter.WriteLine(expected);
            await Assert.That(index).IsLessThan(tokens.Count);
            var actual = $"{tokens[index]}";
            if (actual != expected)
            {
                TestContext.Current?.OutputWriter.WriteLine($"Expected: {expected}");
                TestContext.Current?.OutputWriter.WriteLine($"Actual:   {actual}");
                await Assert.That(actual).IsEqualTo(expected);
            }
            index++;
        }
    }
}