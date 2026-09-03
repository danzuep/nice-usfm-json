namespace USFM.Visitors;

public interface IUSFMVisitor<TResult, in TContext>
{
    TResult Visit(IUsfmNode node, TContext context);
}

public static class IUSFMVisitorExtensions
{
    public static TResult Accept<TResult, TContext>(this IUsfmNode node, IUSFMVisitor<TResult, TContext> visitor, TContext context)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(visitor);
        return visitor.Visit(node, context);
    }
}