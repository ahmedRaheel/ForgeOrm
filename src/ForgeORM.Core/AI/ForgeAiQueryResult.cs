using System.Data;
using System.Text;

namespace ForgeORM.Core;

/// <summary>
/// Result returned from db.AI.QueryAsync. Rows are dynamic dictionaries because AI projections are not known at compile time.
/// </summary>
public sealed record ForgeAiQueryResult(
    string Prompt,
    string Sql,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Explanation);
