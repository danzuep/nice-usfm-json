namespace USFM.Parsers;

/// <summary>
/// Defines a generic parsing strategy that operates over a specific lexer strategy.
/// </summary>
public interface IParserStrategy<TLexer, TResult>
    where TLexer : allows ref struct
{
    TResult Parse(ref TLexer lexer);
}

/// <summary>
/// High-performance generic parsing coordinator.
/// </summary>
public ref struct ParserEngine<TLexer, TParser, TResult>
    where TLexer : allows ref struct
    where TParser : IParserStrategy<TLexer, TResult>, allows ref struct
{
    private TLexer _lexer;
    private TParser _parserStrategy;

    public ParserEngine(TLexer lexer, TParser parserStrategy)
    {
        _lexer = lexer;
        _parserStrategy = parserStrategy;
    }

    public TResult Parse()
    {
        return _parserStrategy.Parse(ref _lexer);
    }
}