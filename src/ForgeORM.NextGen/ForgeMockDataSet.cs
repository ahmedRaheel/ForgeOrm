using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeMockDataSet
{
    private readonly Dictionary<Type, IList> _tables = [];

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public ForgeMockDataSet Add<T>(IEnumerable<T> rows)
    {
        _tables[typeof(T)] = rows.ToList();
        return this;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IReadOnlyList<T> Get<T>()
    {
        if (_tables.TryGetValue(typeof(T), out var rows))
            return rows.Cast<T>().ToList();

        return [];
    }
}
