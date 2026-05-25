using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>Profile entry for query execution.</summary>
public sealed record ForgeQueryBuilderProfileEntry(string Name, string Sql, DateTimeOffset StartedAtUtc, TimeSpan Duration, int Rows);
