using USJ;

namespace USFM;

public static class UsjSpanParser
{
    public static IReadOnlyList<IUsjNode> ParseLineToUsj(ReadOnlySpan<char> line)
    {
        var rootNodes = new List<IUsjNode>();
        var tokenizer = new UsfmTokenizer(line);

        while (tokenizer.MoveNext(out var token))
        {
            if (token.Type == UsfmTokenType.Marker)
            {
                // 1. Handle Verse Markers (\v)
                if (token.Value.SequenceEqual("v"))
                {
                    if (tokenizer.MoveNext(out var nextToken) && nextToken.Type == UsfmTokenType.Text)
                    {
                        rootNodes.Add(new UsjVerse(nextToken.Value.Trim().ToString(), null, null));
                    }
                }
                // 2. Handle Opening Character Styles (\va, \vp)
                else if (token.Value.Length > 0 && token.Value[token.Value.Length - 1] != '*')
                {
                    string style = token.Value.ToString();
                    var charContent = new List<IUsjNode>();

                    // Consume nested tokens inside this char style until its matching closing marker
                    while (tokenizer.MoveNext(out var innerToken))
                    {
                        if (innerToken.Type == UsfmTokenType.Marker &&
                            innerToken.Value.Length > 0 &&
                            innerToken.Value[innerToken.Value.Length - 1] == '*')
                        {
                            // Encountered a closer (e.g. \va* or \vp*), break out of context
                            break;
                        }
                        else if (innerToken.Type == UsfmTokenType.Text)
                        {
                            charContent.Add(new UsjText(innerToken.Value.ToString()));
                        }
                    }

                    rootNodes.Add(new UsjChar(charContent, style));
                }
            }
            else if (token.Type == UsfmTokenType.Text)
            {
                // 3. Handle un-marked text strings outside of character bounds
                string textStr = token.Value.ToString();
                if (!string.IsNullOrWhiteSpace(textStr))
                {
                    rootNodes.Add(new UsjText(textStr));
                }
            }
        }

        return rootNodes.ToArray();
    }
}