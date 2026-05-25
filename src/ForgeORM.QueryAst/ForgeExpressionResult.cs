using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

internal sealed class ForgeExpressionResult
{
    public required string Sql { get; init; }
    public Dictionary<string, object?> Parameters { get; init; } = [];
}
