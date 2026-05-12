namespace USFM;

public sealed class ParsingContext
{
    public string Book { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
    public string Verse { get; set; } = string.Empty;

    public void Reset()
    {
        Book = string.Empty;
        Chapter = string.Empty;
        Verse = string.Empty;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Verse) &&
            string.IsNullOrEmpty(Chapter))
            return Book;
        if (string.IsNullOrEmpty(Verse))
            return $"{Book} {Chapter}";
        return $"{Book} {Chapter}:{Verse}";
    }
}
