using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>Rendered SQL plus parameters.</summary>
public sealed record ForgeRenderedQuery(string Sql, IReadOnlyDictionary<string, object?> Parameters);
