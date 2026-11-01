using System.Linq.Expressions;

namespace LearnQ.Presentation.Components.Shared;

public static class ExpressionExtensions
{
    public static string GetFieldName<T>(this Expression<Func<T>> expression) =>
        expression.Body is MemberExpression member ? member.Member.Name : "";
}