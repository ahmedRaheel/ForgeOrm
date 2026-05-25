using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>Query analysis output with suggested indexes.</summary>
public sealed record ForgeQueryBuilderAnalysis(string Entity, string Sql, IReadOnlyList<string> SuggestedIndexes, IReadOnlyList<string> Notes);
