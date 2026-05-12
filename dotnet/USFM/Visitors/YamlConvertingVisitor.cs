using System.Text;

namespace USFM.Visitors;

public class YamlConvertingVisitor : IUsfmVisitor
{
    private const int BaseIndent = 2;
    private int _indent = 0;
    private readonly ParsingContext _currentVid = new();
    private readonly StringBuilder _sb = new();

    private void WriteLine(string text) =>
        _sb.Append(' ', _indent).AppendLine(text);

    private void WriteContent(IList<IUsfmNode>? content)
    {
        if (content == null || content.Count == 0) return;

        WriteLine("content:");
        _indent += BaseIndent;
        foreach (var node in content)
        {
            // Text nodes are written as list items but without the "type" key
            if (node is TextNode textNode)
                WriteLine($"- \"{Escape(textNode.Text)}\"");
            else
            {
                // For complex nodes, the '-' starts the object
                _sb.Append(' ', _indent).Append("- ");
                _indent += BaseIndent; // Offset for keys inside the list item
                this.Accept(node);
                _indent -= BaseIndent;
            }
        }
        _indent -= BaseIndent;
    }

    public void Visit(BookNode node)
    {
        _currentVid.Book = node.Code;
        _currentVid.Chapter = string.Empty;
        _currentVid.Verse = string.Empty;
        _sb.AppendLine($"type: \"book:{node.Style}\"");
        WriteLine($"code: {node.Code}");
    }

    public void Visit(ChapterNode node)
    {
        _currentVid.Chapter = node.Number;
        _currentVid.Verse = string.Empty;
        WriteLine("- type: \"chapter:c\"");
        _indent += BaseIndent;
        WriteLine($"number: \"{node.Number}\"");
        WriteLine($"sid: \"{_currentVid}\"");
        _indent -= BaseIndent;
    }

    public void Visit(VerseNode node)
    {
        _currentVid.Verse = node.Number;
        WriteLine("- type: \"verse:v\"");
        _indent += BaseIndent;
        WriteLine($"number: \"{node.Number}\"");
        WriteLine($"sid: \"{_currentVid}\"");
        _indent -= BaseIndent;
    }

    public void Visit(ParaNode node)
    {
        WriteLine($"- type: \"para:{node.Style}\"");
        _indent += BaseIndent;
        if (_currentVid != null) WriteLine($"vid: \"{_currentVid}\"");

        if (node.Content?.Any() == true)
        {
            WriteLine("content:");
            _indent += BaseIndent;
            foreach (var child in node.Content) VisitChild(child);
            _indent -= BaseIndent;
        }
        _indent -= BaseIndent;
    }

    private void VisitChild(IUsfmNode node)
    {
        if (node is TextNode t)
        {
            WriteLine($"- \"{t.Text.Replace("\"", "\\\"")}\"");
        }
        else
        {
            // For nested types, the '-' is handled by the specific Visit call
            this.Accept(node);
        }
    }

    public void Visit(CharNode node)
    {
        _sb.AppendLine($"type: \"char:{node.Style}\"");
        // Example of handling extra attributes (if you add them to your CharNode)
        // WriteLine("x-myattr: metadata"); 
        WriteContent(node.Content);
    }

    public void Visit(TextNode node)
    {
        // Handled inside WriteContent for proper list formatting
    }

    public void Visit(NoteNode node)
    {
        _sb.AppendLine($"type: \"note:{node.Style}\"");
        WriteLine($"caller: \"{node.Caller}\"");
        WriteContent(node.Content);
    }

    // Boilerplate for remaining nodes...
    public void Visit(TableNode node) { _sb.AppendLine("type: \"table\""); WriteContent(node.Content); }
    public void Visit(RowNode node) { _sb.AppendLine("type: \"row\""); WriteContent(node.Content); }
    public void Visit(CellNode node) { _sb.AppendLine($"type: \"cell:{node.Style}\""); WriteContent(node.Content); }
    public void Visit(MilestoneNode node) { _sb.AppendLine($"type: \"ms:{node.Style}\""); }
    public void Visit(LineBreakNode node) { _sb.AppendLine("type: \"break\""); }

    private string Escape(string text) => text.Replace("\"", "\\\"");

    public string GetResult() =>
        _sb.ToString();

    public string FinalizeResult()
    {
        var result = GetResult();
        _sb.Clear();
        _currentVid.Reset();
        _indent = 0;
        return result;
    }
}
