using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace ForgeORM.Core.Materialization;

/// <summary>
/// JSON-friendly query result used by reports, dynamic queries and analytics projections.
/// </summary>
public sealed class ForgeJsonProjection
{
    public string? Name { get; init; }

    public string Sql { get; init; } = string.Empty;

    public int RowCount { get; init; }

    public IReadOnlyList<Dictionary<string, object?>> Rows { get; init; } =
        Array.Empty<Dictionary<string, object?>>();

    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
