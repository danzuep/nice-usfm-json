using System.Text;
using USJ;

namespace Bible.Usx.Services
{
    public class UsjToMarkdownVisitor : IUsjVisitor
    {
        public static string GetFullText(UsjIdentification? usjBook)
        {
            var visitor = new UsjToMarkdownVisitor();
            return visitor.Build(usjBook);
        }

        //private readonly List<UsjFootnote> _footnotes = new();
        private readonly List<UsjNote> _footnotes = new();
        private readonly StringBuilder _builder = new();

        public UsjToMarkdownVisitor() { }

        public string Build(UsjIdentification? usxBook)
        {
            this.Accept(usxBook);

            if (_footnotes.Any())
            {
                _builder.AppendLine();
                for (var i = 0; i < _footnotes.Count; i++)
                {
                    AppendFootnote(i);
                }
            }

            return _builder.ToString();
        }

        public void Visit(UsjIdentification book)
        {
            if (!string.IsNullOrWhiteSpace(book.VersionDescription))
                _builder.Append($"# {book.VersionDescription}");
            if (!string.IsNullOrWhiteSpace(book.TranslationCode))
                _builder.Append($" - {book.TranslationCode}");
            _builder.AppendLine();
            _builder.AppendLine();
        }

        public void Visit(UsjChapter marker)
        {
            if (!string.IsNullOrEmpty(marker.Number))
            {
                _builder.AppendFormat("### Chapter {0}", marker.Number);
            }
            _builder.AppendLine();
        }

        public void Visit(UsjVerse marker)
        {
            if (!string.IsNullOrEmpty(marker.Number))
            {
                _builder.AppendFormat("**{0}** ", marker.Number);
            }
        }

        public static readonly IReadOnlyList<string> ParaStylesToHide =
            ["ide", "toc", "mt"];

        public void Visit(UsjPara para)
        {
            if (!string.IsNullOrEmpty(para.Style) &&
                para.Style.StartsWith("h", StringComparison.OrdinalIgnoreCase) &&
                para.Text is string heading)
            {
                _builder.AppendLine($"## {heading}");
                _builder.AppendLine();
            }
            else if (string.IsNullOrEmpty(para.Style) ||
                !ParaStylesToHide.Any(p => para.Style.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                this.Accept(para.Content);
                _builder.AppendLine();
            }
        }

        public void Visit(UsjChar charNode)
        {
            // TODO: add markdown formatting based on style
            this.Accept(charNode.Content);
        }

        public void Visit(UsjText text)
        {
            _builder.Append(text.Text);
        }

        public void Visit(UsjMilestone milestone)
        {
            // Milestones can be represented as inline comments or ignored
            // For demo, let's add a comment
            if (!string.IsNullOrEmpty(milestone.Style))
            {
                _builder.Append($"<!-- Milestone: {milestone.Style} -->");
            }
        }

        public void Visit(UsjLineBreak lineBreak)
        {
            // Markdown line break: two spaces + newline
            _builder.Append("  \n");
        }

        public void Visit(UsjNote note)
        {
            if (note.Style == "x")
            {
                _builder.Append("[");
                this.Accept(note.Content);
                _builder.AppendFormat("({0})]", note.Caller);
            }
            else if (note.Style == "f")
            {
                var index = _footnotes.Count + 1;
                _builder.AppendFormat($"[^{index}]");
                _footnotes.Add(note);
            }
        }

        private void AppendFootnote(int index)
        {
            if (_footnotes[index].Content is IEnumerable<IUsjNode> items)
            {
                _builder.Append($"[^{index + 1}]: ");
                foreach (var item in items)
                {
                    this.Accept(item);
                }
                _builder.AppendLine();
            }
        }
    }
}