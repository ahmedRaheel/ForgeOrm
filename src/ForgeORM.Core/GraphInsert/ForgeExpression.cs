using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static class ForgeExpression
{
    /// <summary>
    /// Executes the Property operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the Property operation.</returns>
    public static PropertyInfo Property(Expression expression)
    {
        while (expression is UnaryExpression unary &&
               (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked || unary.NodeType == ExpressionType.TypeAs))
            expression = unary.Operand;

        if (expression is MemberExpression { Member: PropertyInfo property })
            return property;

        throw new NotSupportedException("Expression must point to a property.");
    }
}
