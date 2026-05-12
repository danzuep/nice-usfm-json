using System.Text;

namespace USFM.Visitors;

public class MarkdownConvertingVisitor : IUsfmVisitor
{
    private readonly List<NoteNode> _footnotes = new();
    private readonly StringBuilder _builder = new();
    private static readonly IReadOnlyList<string> ParaStylesToHide = ["ide", "toc", "mt"];

    public void Visit(BookNode node)
    {
        _builder.AppendFormat("## {0}", node.Code).AppendLine();
    }

    public void Visit(ChapterNode node)
    {
        _builder.AppendLine().AppendFormat("### Chapter {0}", node.Number).AppendLine();
    }

    public void Visit(VerseNode node)
    {
        _builder.AppendFormat("**{0}** ", node.Number);
    }

    public void Visit(ParaNode node)
    {
        if (!string.IsNullOrEmpty(node.Style) &&
            node.Style.StartsWith("h", StringComparison.OrdinalIgnoreCase) &&
            node.Content?.FirstOrDefault() is TextNode textNode)
        {
            _builder.AppendLine($"## {textNode.Text}").AppendLine();
        }
        else if (string.IsNullOrEmpty(node.Style) ||
            !ParaStylesToHide.Any(p => node.Style.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            this.Accept(node.Content);
            _builder.AppendLine();
        }
    }

    public void Visit(NoteNode node)
    {
        if (node.Style == "f")
        {
            var index = _footnotes.Count + 1;
            _builder.AppendFormat($"[^{index}]");
            _footnotes.Add(node);
        }
        else if (node.Style == "x")
        {
            _builder.Append("[");
            this.Accept(node.Content);
            _builder.AppendFormat("({0})]", node.Caller);
        }
    }

    public void Visit(CharNode node) => this.Accept(node.Content);
    public void Visit(TextNode node) => _builder.Append(node.Text);
    public void Visit(MilestoneNode node) => _builder.Append($"<!-- {node.Style} -->");
    public void Visit(LineBreakNode node) => _builder.Append("  \n");
    public void Visit(TableNode node) => this.Accept(node.Content);
    public void Visit(RowNode node) => this.Accept(node.Content);
    public void Visit(CellNode node) => this.Accept(node.Content);

    public string GetResult() => _builder.ToString();

    public string FinalizeResult()
    {
        if (_footnotes.Any())
        {
            _builder.AppendLine();
            for (var i = 0; i < _footnotes.Count; i++)
            {
                _builder.Append($"[^{i + 1}]: ");
                this.Accept(_footnotes[i].Content);
                _builder.AppendLine();
            }
            _footnotes.Clear();
        }
        var result = GetResult();
        _builder.Clear();
        return result;
    }
}