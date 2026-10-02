using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Turns selector lambdas such as <c>p => p.StartDate</c> into the property they name, so report
/// parameters and columns are declared with compile-checked member access instead of strings.
/// </summary>
internal static class ReportExpressions
{
    public static PropertyInfo GetSettableProperty<T, TValue>(Expression<Func<T, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return FindProperty(expression) is { SetMethod.IsPublic: true } property
            ? property
            : throw new ArgumentException(
                "The expression must select a public settable property, for example p => p.StartDate.",
                nameof(expression));
    }

    public static string? GetPropertyKey<T, TValue>(Expression<Func<T, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return FindProperty(expression) is { } property ? ToKey(property.Name) : null;
    }

    /// <summary>
    /// Keys use the same camelCase naming as the JSON the client sends and receives.
    /// </summary>
    public static string ToKey(string name)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(name);
    }

    private static PropertyInfo? FindProperty(LambdaExpression expression)
    {
        return expression.Body is MemberExpression { Member: PropertyInfo property, Expression: ParameterExpression }
            ? property
            : null;
    }
}
