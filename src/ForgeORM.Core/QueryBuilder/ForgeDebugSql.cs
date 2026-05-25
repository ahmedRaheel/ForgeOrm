using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>Debug SQL representation.</summary>
public sealed record ForgeDebugSql(string Sql, IReadOnlyDictionary<string, object?> Parameters);
