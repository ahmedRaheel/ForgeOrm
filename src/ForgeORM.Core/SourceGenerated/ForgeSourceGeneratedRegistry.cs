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
/// Registry for source-generated providers. Registration happens once via ModuleInitializer.
/// Lookup is cached by entity/DTO type to avoid provider scans on repeated calls.
/// </summary>
public static class ForgeSourceGeneratedRegistry
{
    private static readonly List<IForgeSourceGeneratedAccessorProvider> Providers = new();
    private static readonly ConcurrentDictionary<Type, IForgeSourceGeneratedAccessorProvider?> ProviderByType = new();
    private static readonly ConcurrentDictionary<Type, ForgeEntityMetadata> MetadataByType = new();
    private static readonly object Gate = new();

    public static ForgeOrmCompilationMode CompilationMode { get; set; } = ForgeOrmCompilationMode.Auto;

    /// <summary>True when at least one entity metadata map has been registered by generated code.</summary>
    public static bool HasGeneratedMetadata => !MetadataByType.IsEmpty;

    /// <summary>Registers source-generated metadata for an entity or projection type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterMetadata(Type type, ForgeEntityMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(metadata);
        MetadataByType[type] = metadata;
    }

    /// <summary>Gets source-generated metadata when available.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetMetadata(Type type, out ForgeEntityMetadata metadata)
        => MetadataByType.TryGetValue(type, out metadata!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Register(IForgeSourceGeneratedAccessorProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (Gate)
        {
            Providers.Add(provider);
            ProviderByType.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetProvider(Type type, out IForgeSourceGeneratedAccessorProvider provider)
    {
        if (ProviderByType.TryGetValue(type, out var cached))
        {
            provider = cached!;
            return provider is not null;
        }

        lock (Gate)
        {
            for (var i = 0; i < Providers.Count; i++)
            {
                var candidate = Providers[i];
                if (!candidate.CanHandle(type))
                    continue;

                ProviderByType[type] = candidate;
                provider = candidate;
                return true;
            }

            ProviderByType[type] = null;
            provider = null!;
            return false;
        }
    }

    /// <summary>Attempts to execute a full source-generated provider-neutral first-row query.</summary>
    public static bool TryExecuteFirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetProvider(typeof(T), out var provider))
        {
            result = default;
            return false;
        }

        return provider.TryExecuteFirstOrDefaultAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken, out result);
    }

    /// <summary>Attempts to execute a full source-generated provider-neutral list query.</summary>
    public static bool TryExecuteQueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<IReadOnlyList<T>> result)
    {
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetProvider(typeof(T), out var provider))
        {
            result = default;
            return false;
        }

        return provider.TryExecuteQueryAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken, out result);
    }

    /// <summary>Attempts to execute a full source-generated provider-neutral scalar query.</summary>
    public static bool TryExecuteScalarAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetProvider(typeof(T), out var provider))
        {
            result = default;
            return false;
        }

        return provider.TryExecuteScalarAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken, out result);
    }

    /// <summary>Attempts to execute a full source-generated provider-neutral non-query command.</summary>
    public static bool TryExecuteNonQueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<int> result)
    {
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetProvider(typeof(T), out var provider))
        {
            result = default;
            return false;
        }

        return provider.TryExecuteNonQueryAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken, out result);
    }

    /// <summary>Attempts to execute a full source-generated SQL Server first-row query with older generated providers.</summary>
    public static bool TryExecuteSqlServerFirstOrDefaultAsync<T>(
        string connectionString,
        string sql,
        object? parameters,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit)
        {
            result = default;
            return false;
        }

        if (TryGetProvider(typeof(T), out var provider) &&
            provider.TryExecuteSqlServerFirstOrDefaultAsync(connectionString, sql, parameters, timeoutSeconds, cancellationToken, out result))
        {
            return true;
        }

        result = default;
        return false;
    }
}
