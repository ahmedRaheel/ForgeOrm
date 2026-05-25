using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Compile-time generated accessor provider. Implemented by ForgeORM.SourceGenerated output.
/// GetReader receives the live reader once so generated code can bind ordinals by column name once,
/// then return a static hot-path delegate with direct typed reads per row.
/// </summary>
public interface IForgeSourceGeneratedAccessorProvider
{
    bool CanHandle(Type type);

    /// <summary>
    /// Legacy object reader path retained for compatibility with older generated providers.
    /// New generators should override TryCreateReader&lt;T&gt; to avoid a cast on every row.
    /// </summary>
    Func<DbDataReader, object> GetReader(Type type, DbDataReader reader);

    /// <summary>
    /// Legacy object binder path retained for compatibility. New generators can override
    /// TryGetBinder to return their generated binder directly.
    /// </summary>
    Action<DbCommand, object> GetBinder(Type type);

    /// <summary>
    /// Preferred typed reader path. The default implementation adapts the legacy object reader once,
    /// so existing generated providers still work. Source-generated providers should emit a typed
    /// implementation to remove per-row casts.
    /// </summary>
    bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader, T>? readerFunc)
    {
        if (!CanHandle(typeof(T)))
        {
            readerFunc = null;
            return false;
        }

        var objectReader = GetReader(typeof(T), reader);
        readerFunc = r => (T)objectReader(r);
        return true;
    }

    /// <summary>Gets a generated parameter binder for the requested parameter/entity type.</summary>
    bool TryGetBinder(Type type, out Action<DbCommand, object>? binder)
    {
        if (!CanHandle(type))
        {
            binder = null;
            return false;
        }

        binder = GetBinder(type);
        return true;
    }

    /// <summary>Gets a generated strongly-typed parameter binder when available.</summary>
    bool TryGetTypedBinder<T>(out IForgeParameterBinder<T>? binder)
    {
        binder = null;
        return false;
    }

    /// <summary>
    /// Optional provider-neutral full query executor generated for hot paths.
    /// When this returns true the generic ForgeORM runtime pipeline is bypassed for any ADO.NET provider.
    /// </summary>
    bool TryExecuteFirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        result = default;
        return false;
    }

    /// <summary>Executes a generated provider-neutral list query for any ADO.NET provider.</summary>
    bool TryExecuteQueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<IReadOnlyList<T>> result)
    {
        result = default;
        return false;
    }

    /// <summary>Executes a generated provider-neutral scalar query for any ADO.NET provider.</summary>
    bool TryExecuteScalarAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        result = default;
        return false;
    }

    /// <summary>Executes a generated provider-neutral non-query command for any ADO.NET provider.</summary>
    bool TryExecuteNonQueryAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<int> result)
    {
        result = default;
        return false;
    }

    /// <summary>
    /// Compatibility SQL Server executor retained for older generated providers.
    /// New generated providers should implement provider-neutral methods above.
    /// </summary>
    bool TryExecuteSqlServerFirstOrDefaultAsync<T>(
        string connectionString,
        string sql,
        object? parameters,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        result = default;
        return false;
    }
}
