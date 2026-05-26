using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Selects how ForgeORM obtains compiled readers, binders and accessors.
/// Auto prefers source-generated code when registered and falls back to RuntimeEmit.
/// NativeAOT users can explicitly select SourceGenerated from configuration.
/// </summary>

/// <summary>
/// Strongly-typed parameter binder used by generated code and high-performance fallback binders.
/// This avoids boxing the parameter object and removes MethodInfo.Invoke from hot paths.
/// </summary>
public interface IForgeParameterBinder<T>
{
    void Bind(DbCommand command, T value);
}

/// <summary>
/// Optional provider-specific materializer hook. Provider packages can register typed readers
/// such as SqlDataReader/NpgsqlDataReader without forcing ForgeORM.Core to reference provider assemblies.
/// </summary>
public interface IForgeProviderMaterializer
{
    bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader, T>? materializer);
}

/// <summary>Registry for provider-specific typed materializers.</summary>
public static class ForgeProviderMaterializerRegistry
{
    private static readonly List<IForgeProviderMaterializer> Providers = new();
    private static readonly object Gate = new();

    public static void Register(IForgeProviderMaterializer provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (Gate) Providers.Add(provider);
    }

    public static bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader, T>? materializer)
    {
        lock (Gate)
        {
            for (var i = 0; i < Providers.Count; i++)
            {
                if (Providers[i].TryCreateReader(reader, out materializer))
                    return true;
            }
        }

        materializer = null;
        return false;
    }
}

public enum ForgeOrmCompilationMode
{
    /// <summary>Prefer source-generated artifacts, but safely fall back to provider-native/MSIL runtime emit when a generated artifact is missing.</summary>
    Auto = 0,

    /// <summary>Force runtime emit/materializer fallback and ignore source-generated providers.</summary>
    RuntimeEmit = 1,

    /// <summary>Force source-generated artifacts and fail fast when a generated reader/binder is missing. RuntimeEmit fallback is disabled.</summary>
    SourceGenerated = 2,

    /// <summary>NativeAOT/strict alias for SourceGenerated-only behavior. Fail fast if any required generated artifact is missing.</summary>
    SourceGeneratedStrict = 3
}

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
    private static int DiscoveryAttempted;

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

        IForgeSourceGeneratedAccessorProvider? found = null;
        lock (Gate)
        {
            for (var i = 0; i < Providers.Count; i++)
            {
                var candidate = Providers[i];
                if (!candidate.CanHandle(type))
                    continue;

                found = candidate;
                break;
            }
        }

        ProviderByType[type] = found;
        provider = found!;
        return provider is not null;
    }

    /// <summary>
    /// Gets a generated provider and, when the provider was not registered yet, actively discovers
    /// the generated provider emitted into the consuming assembly. This is the correct SourceGenerated
    /// behavior: SourceGenerated means create/use generated code, not silently fall back to RuntimeEmit.
    /// </summary>
    public static bool TryGetOrCreateProvider(Type type, out IForgeSourceGeneratedAccessorProvider provider)
    {
        if (TryGetProvider(type, out provider))
            return true;

        DiscoverGeneratedProviders();
        return TryGetProvider(type, out provider);
    }

    private static void DiscoverGeneratedProviders()
    {
        if (Interlocked.Exchange(ref DiscoveryAttempted, 1) == 1)
            return;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < assemblies.Length; i++)
        {
            var assembly = assemblies[i];
            Type?[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            catch
            {
                continue;
            }

            for (var j = 0; j < types.Length; j++)
            {
                var providerType = types[j];
                if (providerType is null || providerType.IsAbstract || providerType.IsInterface)
                    continue;

                if (!typeof(IForgeSourceGeneratedAccessorProvider).IsAssignableFrom(providerType))
                    continue;

                // The generated provider usually registers itself through ModuleInitializer.
                // This fallback handles cases where the generated assembly is loaded but the
                // provider was not in the registry due to project/reference configuration.
                try
                {
                    if (Activator.CreateInstance(providerType) is IForgeSourceGeneratedAccessorProvider provider)
                        Register(provider);
                }
                catch
                {
                    // Ignore bad provider candidates; SourceGenerated/Strict will still fail
                    // with a clear error if no generated provider can handle the requested type.
                }
            }
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
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetOrCreateProvider(typeof(T), out var provider))
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
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetOrCreateProvider(typeof(T), out var provider))
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
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetOrCreateProvider(typeof(T), out var provider))
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
        if (CompilationMode == ForgeOrmCompilationMode.RuntimeEmit || !TryGetOrCreateProvider(typeof(T), out var provider))
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

        if (TryGetOrCreateProvider(typeof(T), out var provider) &&
            provider.TryExecuteSqlServerFirstOrDefaultAsync(connectionString, sql, parameters, timeoutSeconds, cancellationToken, out result))
        {
            return true;
        }

        result = default;
        return false;
    }
}
