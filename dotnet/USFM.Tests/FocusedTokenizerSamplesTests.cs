using USFM.Lexers;

namespace USFM.Tests;

public class FocusedTokenizerSamplesTests
{
    private static string SamplePath(string sample)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "samples")))
        {
            dir = dir.Parent;
        }
        if (dir == null) throw new FileNotFoundException("Could not locate samples directory");
        return Path.Combine(dir.FullName, "samples", sample, "origin.usfm");
    }

    private static List<(string Type, string Value, string Extra)> Tokenize(string input)
    {
        var tokens = new List<(string, string, string)>();
        var tokenizer = new UsfmLexerStrategy(input.AsSpan());
        while (tokenizer.TryMoveNext(out var token))
        {
            tokens.Add((token[0].ToString().TrimStart('\\').TrimEnd(' '), token[1].ToString(), token[2].ToString()));
        }
        return tokens;
    }

    private static string ToStringToken((string type, string value, string extra) token) => $"{token.type}{token.value}{token.extra}";

    [Test]
    public async Task Tokenize_CrossRefs_Sample()
    {
        var all = File.ReadAllLines(SamplePath("cross-refs"));
        var line = all.First(l => l.StartsWith("\\v 3"));
        var parts = Tokenize(line);

        // The x marker should be tokenized with its content and closing
        var x = parts.FirstOrDefault(p => p.Type == "x");
        await Assert.That(x.Type).IsEqualTo("x");
        await Assert.That(x.Value).Contains("- ");
    }

    [Test]
    public async Task Tokenize_Milestones_Sample()
    {
        var all = File.ReadAllLines(SamplePath("milestones"));
        var line = all.First(l => l.StartsWith("\\qt-s"));
        var token = Tokenize(line).Single();

        await Assert.That(token.Type).IsEqualTo("\\qt-s");
        await Assert.That(token.Extra).IsEqualTo("\\*");
        await Assert.That(ToStringToken(token)).IsEqualTo(line);
    }

    [Test]
    public async Task Tokenize_Footnote_Sample()
    {
        var lines = File.ReadAllLines(SamplePath("footnote"));
        var line = lines.First(l => l.StartsWith("\\v 3"));
        var parts = Tokenize(line);

        // Ensure we see a footnote marker \f and that its value contains the \fr and \ft submarkers
        var f = parts.FirstOrDefault(p => p.Type == "f");
        await Assert.That(f.Type).IsEqualTo("f");
        await Assert.That(f.Value).Contains("\\fr");
        await Assert.That(f.Value).Contains("\\ft");
    }

    [Test]
    public async Task Tokenize_DefaultAttributes_Sample()
    {
        var all = File.ReadAllLines(SamplePath("default-attributes"));
        var line = all.First(l => l.Contains("\\w "));
        var parts = Tokenize(line);

        // The w marker with closing w* should be a single combined token
        var w = parts.FirstOrDefault(p => p.Type.StartsWith("\\w"));
        await Assert.That(w.Type).IsEqualTo("\\w");
        await Assert.That(w.Value).Contains("grace");
        await Assert.That(w.Extra).IsEqualTo("\\w*");
    }

    [Test]
    public async Task Tokenize_Table_Sample()
    {
        var all = File.ReadAllLines(SamplePath("table"));
        var line = all.First(l => l.StartsWith("\\tr "));
        var parts = Tokenize(line);

        // ensure row and header markers are present and not duplicated
        var tr = parts.FirstOrDefault(p => p.Type.StartsWith("\\tr"));
        await Assert.That(tr.Type).IsEqualTo("\\tr");
        await Assert.That(!parts.Any(p => p.Type.StartsWith("\\tr\\"))).IsTrue();
        await Assert.That(parts.Any(p => p.Type.StartsWith("\\th1"))).IsTrue();
    }
}