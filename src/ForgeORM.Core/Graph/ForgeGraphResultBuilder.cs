using System.Diagnostics;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Builds graph operation statistics.
/// </summary>
public sealed class ForgeGraphResultBuilder
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Dictionary<string, int> _rowsByTable = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ForgeBulkStrategy> _strategyByTable = new(StringComparer.OrdinalIgnoreCase);
    private int _inserted;
    private int _updated;
    private int _deleted;

    /// <summary>
    /// Adds an inserted row count.
    /// </summary>
    public void AddInserted(string tableName, int count, ForgeBulkStrategy strategy)
    {
        _inserted += count;
        AddTable(tableName, count, strategy);
    }

    /// <summary>
    /// Adds an updated row count.
    /// </summary>
    public void AddUpdated(string tableName, int count, ForgeBulkStrategy strategy)
    {
        _updated += count;
        AddTable(tableName, count, strategy);
    }

    /// <summary>
    /// Adds a deleted row count.
    /// </summary>
    public void AddDeleted(string tableName, int count, ForgeBulkStrategy strategy)
    {
        _deleted += count;
        AddTable(tableName, count, strategy);
    }

    /// <summary>
    /// Creates the immutable graph result.
    /// </summary>
    public ForgeGraphResult Build()
    {
        _stopwatch.Stop();
        return new ForgeGraphResult
        {
            TotalInserted = _inserted,
            TotalUpdated = _updated,
            TotalDeleted = _deleted,
            Duration = _stopwatch.Elapsed,
            RowsByTable = new Dictionary<string, int>(_rowsByTable),
            StrategyByTable = new Dictionary<string, ForgeBulkStrategy>(_strategyByTable)
        };
    }

    private void AddTable(string tableName, int count, ForgeBulkStrategy strategy)
    {
        _rowsByTable[tableName] = _rowsByTable.TryGetValue(tableName, out var existing)
            ? existing + count
            : count;

        _strategyByTable[tableName] = strategy;
    }
}
