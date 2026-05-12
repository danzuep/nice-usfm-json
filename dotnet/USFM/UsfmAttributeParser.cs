public partial class UsfmParser
{
    public static class UsfmAttributeParser
    {
        public static Dictionary<string, string> Parse(ReadOnlySpan<char> input, out int endMarkerIndex)
        {
            var attributes = new Dictionary<string, string>();
            endMarkerIndex = -1;

            int start = input.IndexOf('|');
            int end = input.IndexOf("\\*");

            if (start == -1) return attributes;

            // Calculate the span containing just the attributes
            var attributeSpan = (end != -1)
                ? input.Slice(start + 1, end - start - 1)
                : input[(start + 1)..];

            endMarkerIndex = end != -1 ? end + 2 : input.Length;

            // Simple regex-free key="value" parser
            int i = 0;
            while (i < attributeSpan.Length)
            {
                while (i < attributeSpan.Length && char.IsWhiteSpace(attributeSpan[i])) i++;
                if (i >= attributeSpan.Length) break;

                int eq = attributeSpan[i..].IndexOf('=');
                if (eq == -1) break;

                string key = attributeSpan.Slice(i, eq).Trim().ToString();
                i += eq + 1;

                if (i < attributeSpan.Length && attributeSpan[i] == '"')
                {
                    i++;
                    int closeQuote = attributeSpan[i..].IndexOf('"');
                    if (closeQuote == -1) break;
                    string value = attributeSpan.Slice(i, closeQuote).ToString();
                    attributes[key] = value;
                    i += closeQuote + 1;
                }
            }
            return attributes;
        }
    }
}