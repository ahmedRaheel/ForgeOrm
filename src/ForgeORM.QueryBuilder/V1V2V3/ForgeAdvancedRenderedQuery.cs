using System.Linq.Expressions;

namespace ForgeORM.QueryBuilder;

public sealed record ForgeAdvancedRenderedQuery(string Sql, object? Parameters);
