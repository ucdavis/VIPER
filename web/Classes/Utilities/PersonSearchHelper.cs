using System.Linq.Expressions;

namespace Viper.Classes.Utilities;

/// <summary>
/// Shared query shape for "search current people by partial name" autocomplete endpoints
/// (phone directory, CMS file/permission pickers): trim + minimum-length guard, match on
/// "Last, First" or "First Last" containing the search term, ordered by last/first name,
/// capped to a page of results. Callers supply their own DbSet/entity and last/first name
/// property selectors; additional match fields (e.g. login id) can be OR'd in via <see cref="Or{T}"/>.
/// </summary>
public static class PersonSearchHelper
{
    public const int MinSearchLength = 2;
    public const int MaxResults = 25;

    /// <summary>
    /// Trims a search term and validates its length. Returns null if the term is too short to
    /// search on, signalling the caller should skip the query and return an empty result.
    /// </summary>
    public static string? Normalize(string? search)
    {
        search = search?.Trim();
        return string.IsNullOrEmpty(search) || search.Length < MinSearchLength ? null : search;
    }

    /// <summary>
    /// Builds the "Last, First" / "First Last" contains-match predicate for a search term.
    /// lastName/firstName must be plain property accessors (e.g. <c>t => t.LastName</c>).
    /// </summary>
    public static Expression<Func<T, bool>> NameMatches<T>(
        Expression<Func<T, string>> lastName,
        Expression<Func<T, string>> firstName,
        string search)
    {
        var param = Expression.Parameter(typeof(T), "p");
        var last = Expression.Property(param, PropertyName(lastName));
        var first = Expression.Property(param, PropertyName(firstName));
        // Read the term off a holder rather than embedding it with Expression.Constant. EF renders
        // a bare constant as a SQL literal (LIKE N'%smith%'), which gives every distinct term its
        // own query plan and skips the ESCAPE clause, so a typed % or _ acts as a wildcard. A field
        // access on a captured object is the shape a C# closure produces, and EF parameterizes it.
        var searchTerm = Expression.Field(Expression.Constant(new SearchTerm(search)), nameof(SearchTerm.Value));
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var concatMethod = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])!;

        var lastCommaFirst = Expression.Call(concatMethod, last, Expression.Constant(", "), first);
        var firstSpaceLast = Expression.Call(concatMethod, first, Expression.Constant(" "), last);

        var body = Expression.OrElse(
            Expression.Call(lastCommaFirst, containsMethod, searchTerm),
            Expression.Call(firstSpaceLast, containsMethod, searchTerm));

        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    /// <summary>
    /// Orders by last name then first name and caps to <see cref="MaxResults"/>.
    /// </summary>
    public static IQueryable<T> OrderAndCap<T>(
        IQueryable<T> query,
        Expression<Func<T, string>> lastName,
        Expression<Func<T, string>> firstName)
        => query.OrderBy(lastName).ThenBy(firstName).Take(MaxResults);

    /// <summary>
    /// ORs an additional match condition (e.g. login id / mail id) onto a predicate built by
    /// <see cref="NameMatches{T}"/>, rebinding it onto the same parameter.
    /// </summary>
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> predicate,
        Expression<Func<T, bool>> other)
    {
        var rebound = new ParameterRebinder(other.Parameters[0], predicate.Parameters[0]).Visit(other.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(predicate.Body, rebound), predicate.Parameters[0]);
    }

    private static string PropertyName<T>(Expression<Func<T, string>> selector)
    {
        if (selector.Body is MemberExpression member)
        {
            return member.Member.Name;
        }
        throw new ArgumentException("Selector must be a simple property accessor.", nameof(selector));
    }

    /// <summary>
    /// Holder whose field the predicate reads the search term from, so EF treats it as a captured
    /// closure variable and emits a SQL parameter instead of a literal. Must stay a field, not a
    /// property: EF parameterizes both, but the field mirrors what the compiler generates.
    /// </summary>
    private sealed class SearchTerm(string value)
    {
        public readonly string Value = value;
    }

    private sealed class ParameterRebinder(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
