using System.Diagnostics.CodeAnalysis;
using Usx.Parsers;

namespace Bible.Usx.Services;

public class UsxParserFactory
{
    private readonly Dictionary<string, Func<IUsxElementParser>> _parserFactories;

    public UsxParserFactory()
    {
        _parserFactories = new(StringComparer.OrdinalIgnoreCase)
        {
            [BookParser.Key] = () => new BookParser(),
            [ChapterMarkerParser.Key] = () => new ChapterMarkerParser(),
            [VerseMarkerParser.Key] = () => new VerseMarkerParser(),
            [MilestoneParser.Key] = () => new MilestoneParser(),
            [LineBreakParser.Key] = () => new LineBreakParser(),
            [FootnoteParser.Key] = () => new FootnoteParser(this),
            [CrossReferenceParser.Key] = () => new CrossReferenceParser(this),
            [ParagraphParser.Key] = () => new ParagraphParser(this),
            [CharacterParser.Key] = () => new CharacterParser(),
            [TextParser.Key] = () => new TextParser(),
        };
    }

    public bool TryGetParser(string elementName, [NotNullWhen(true)] out IUsxElementParser? parser)
    {
        if (_parserFactories.TryGetValue(elementName, out var parserFunc))
        {
            parser = parserFunc();
            return true;
        }
        parser = null;
        return false;
    }

    public bool HasTextEnrichment { get; private set; }
    public void SetTextParser(Func<int, IList<string>>? enrich = null)
    {
        _parserFactories[TextParser.Key] = () => new TextParser(enrich);
        HasTextEnrichment = true;
    }

    public TextParser TextParser => TryGetParser(TextParser.Key, out var parser) && parser is TextParser tp ? tp: new TextParser();
}