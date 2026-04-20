using USJ;

namespace Bible.Usx.Services
{
    public interface IUsjVisitor
    {
        void Visit(UsjIdentification book);
        void Visit(UsjChapter chapter);
        void Visit(UsjVerse verse);
        void Visit(UsjChar metatext);
        void Visit(UsjPara heading);
        void Visit(UsjText text);
        void Visit(UsjMilestone milestone);
        void Visit(UsjLineBreak lineBreak);
        void Visit(UsjNote note);
    }

    public static class UsjVisitorExtensions
    {
        public static void Accept(this IUsjVisitor visitor, IUsjNode? usjNode)
        {
            if (usjNode == null) return;
            switch (usjNode)
            {
                case UsjText s: visitor.Visit(s); break;
                case UsjChar w: visitor.Visit(w); break;
                case UsjPara p: visitor.Visit(p); break;
                case UsjVerse v: visitor.Visit(v); break;
                case UsjChapter c: visitor.Visit(c); break;
                case UsjNote n: visitor.Visit(n); break;
                case UsjLineBreak br: visitor.Visit(br); break;
                case UsjMilestone ms: visitor.Visit(ms); break;
                case UsjIdentification b: visitor.Accept(b); break;
                default:
                    throw new NotSupportedException($"Unknown USX type: {usjNode.GetType()}");
            }
        }

        public static void Accept(this IUsjVisitor visitor, IEnumerable<IUsjNode>? content)
        {
            if (content == null) return;
            foreach (var item in content)
                visitor.Accept(item);
        }
    }
}