namespace USFM.Lexers;

public interface ILexerStrategy
{
    bool TryMoveNext(out LexerToken token);
}
