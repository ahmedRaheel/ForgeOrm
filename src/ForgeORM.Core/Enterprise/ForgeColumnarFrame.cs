using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Columnar analytics frame foundation.
/// </summary>
public sealed class ForgeColumnarFrame
{
    private readonly Dictionary<string, List<object?>> _columns = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyList<object?>> Columns =>
        _columns.ToDictionary(x => x.Key, x => (IReadOnlyList<object?>)x.Value, StringComparer.OrdinalIgnoreCase);

    public int RowCount => _columns.Count == 0 ? 0 : _columns.Values.Max(x => x.Count);

    public void AddColumn(string name, IEnumerable<object?> values)
        => _columns[name] = values.ToList();

    public decimal Sum(string column)
        => _columns.TryGetValue(column, out var values)
            ? values.Where(x => x is not null).Sum(x => Convert.ToDecimal(x))
            : 0m;
}
