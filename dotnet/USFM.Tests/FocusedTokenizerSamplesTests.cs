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

    [Test]
    public async Task Tokenize_CrossRefs_Sample()
    {
        var all = File.ReadAllLines(SamplePath("cross-refs"));
        var line = all.First(l => l.StartsWith("\\v 3"));
        var tokenizer = new UsfmTokenizer(line.AsSpan());
        var parts = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var t)) parts.Add((t.Type.ToString(), t.Value.ToString()));

        await Assert.That(parts.Any(p => p.Type == "x")).IsTrue();
        await Assert.That(parts.Any(p => p.Type == "xo")).IsTrue();
        // ensure trailing inline text after \x* is preserved somewhere
        var x = parts.FirstOrDefault(p => p.Type == "x");
        await Assert.That(x.Value).Contains("- ");
    }

    [Test]
    public async Task Tokenize_Milestones_Sample()
    {
        var all = File.ReadAllLines(SamplePath("milestones"));
        var line = all.First(l => l.StartsWith("\\qt-s"));
        var tokenizer = new UsfmTokenizer(line.AsSpan());
        var parts = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var t)) parts.Add((t.Type.ToString(), t.Value.ToString()));

        var qt = parts.FirstOrDefault(p => p.Type.StartsWith("qt-s"));
        await Assert.That(qt.Type).Contains("qt-s");
        await Assert.That(qt.Value).Contains("sid=");
        // ensure the closing delimiter \* is present in the value
        await Assert.That(qt.Value).Contains("\\*");
    }

    [Test]
    public async Task Tokenize_Footnote_Sample()
    {
        var lines = File.ReadAllLines(SamplePath("footnote"));
        var line = lines.First(l => l.StartsWith("\\v 3"));
        var tokenizer = new UsfmTokenizer(line.AsSpan());
        var parts = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var t)) parts.Add((t.Type.ToString(), t.Value.ToString()));

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
        var tokenizer = new UsfmTokenizer(line.AsSpan());
        var parts = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var t)) parts.Add((t.Type.ToString(), t.Value.ToString()));

        var w = parts.FirstOrDefault(p => p.Type == "w");
        await Assert.That(w.Type).IsEqualTo("w");
        await Assert.That(w.Value).Contains("grace");
        // Ensure there is a separate closing token for the w marker (type "w*")
        await Assert.That(parts.Any(p => p.Type == "w*")).IsTrue();
    }

    [Test]
    public async Task Tokenize_Table_Sample()
    {
        var all = File.ReadAllLines(SamplePath("table"));
        var line = all.First(l => l.StartsWith("\\tr "));
        var tokenizer = new UsfmTokenizer(line.AsSpan());
        var parts = new List<(string Type, string Value)>();
        while (tokenizer.TryMoveNext(out var t)) parts.Add((t.Type.ToString(), t.Value.ToString()));

        // ensure row and header markers are present and not duplicated
        await Assert.That(parts.Any(p => p.Type == "tr")).IsTrue();
        await Assert.That(parts.Count(p => p.Type == "tr")).IsEqualTo(1);
        await Assert.That(parts.Any(p => p.Type == "th1")).IsTrue();
    }
}
