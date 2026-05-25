using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ForgeORM.Core;

/// <summary>
/// Query analysis result returned by ForgeQueryBuilder.Analyze().
/// </summary>
public sealed class ForgeQueryAnalysis
{
    public string Entity { get; init; } = string.Empty;

    public string TableName { get; init; } = string.Empty;

    public string Sql { get; init; } = string.Empty;

    public IReadOnlyList<string> WhereColumns { get; init; } = [];

    public IReadOnlyList<string> OrderByColumns { get; init; } = [];

    public IReadOnlyList<string> SuggestedIndexes { get; init; } = [];

    public IReadOnlyList<string> Notes { get; init; } = [];
}
