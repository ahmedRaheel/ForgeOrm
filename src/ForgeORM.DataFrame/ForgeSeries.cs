using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeORM.DataFrame;

/// <summary>
/// A lightweight Pandas-like Series abstraction used by ForgeORM analytics.
/// </summary>
public sealed class ForgeSeries
{
    private readonly List<object?> _values;

    /// <summary>
    /// Creates a new series.
    /// </summary>
    public ForgeSeries(string name, IEnumerable<object?> values)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Value" : name;
        _values = values?.ToList() ?? [];
    }

    /// <summary>The series name.</summary>
    public string Name { get; }

    /// <summary>The series values.</summary>
    public IReadOnlyList<object?> Values => _values;

    /// <summary>Returns unique values in order of first occurrence.</summary>
    public IReadOnlyList<object?> Unique() => _values.Distinct(ForgeObjectEqualityComparer.Instance).ToList();

    /// <summary>Returns the number of unique values.</summary>
    public int NUnique(bool dropNa = true) => dropNa
        ? _values.Where(v => v is not null and not DBNull).Distinct(ForgeObjectEqualityComparer.Instance).Count()
        : _values.Distinct(ForgeObjectEqualityComparer.Instance).Count();

    /// <summary>Returns value counts as a dataframe with Value and Count columns.</summary>
    public ForgeDataFrame ValueCounts(bool dropNa = true)
    {
        var rows = _values
            .Where(v => !dropNa || v is not null and not DBNull)
            .GroupBy(v => Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                [Name] = g.First(),
                ["Count"] = g.Count()
            });
        return new ForgeDataFrame(rows);
    }

    /// <summary>Returns the first n values as a series.</summary>
    public ForgeSeries Head(int count = 5) => new(Name, _values.Take(Math.Max(count, 0)));

    /// <summary>Returns the last n values as a series.</summary>
    public ForgeSeries Tail(int count = 5) => new(Name, _values.Skip(Math.Max(0, _values.Count - Math.Max(count, 0))));

    /// <summary>Returns the series as a single-column dataframe.</summary>
    public ForgeDataFrame ToDataFrame() => new(_values.Select(v => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [Name] = v }));
}
