using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed class ForgeGridReader : IForgeGridReader
{
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbDataReader _reader;
    private bool _hasConsumedCurrentResult;

    /// <summary>
    /// Executes the ForgeGridReader operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="command">The command value.</param>
    /// <param name="reader">The reader value.</param>
    /// <returns>The result of the ForgeGridReader operation.</returns>
    public ForgeGridReader(DbConnection connection, DbCommand command, DbDataReader reader)
    {
        _connection = connection;
        _command = command;
        _reader = reader;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IEnumerable<T> Read<T>() => ReadAsync<T>().GetAwaiter().GetResult();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask<IReadOnlyList<T>> ReadAsync<T>()
    {
        if (_hasConsumedCurrentResult)
            await _reader.NextResultAsync();

        var rows = new List<T>();
        var materializer = ForgeIlMaterializerCache.GetOrCreate<T>(_reader);
        while (await _reader.ReadAsync())
            rows.Add(materializer(_reader));

        _hasConsumedCurrentResult = true;
        return rows;
    }

    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
        _reader.Dispose();
        _command.Dispose();
        _connection.Dispose();
    }
}
