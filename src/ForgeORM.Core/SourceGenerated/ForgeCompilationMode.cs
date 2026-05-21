using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

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
    Auto = 0,
    RuntimeEmit = 1,
    SourceGenerated = 2
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
}

/// <summary>
/// Registry for source-generated providers. Registration happens once via ModuleInitializer.
/// Lookup is cached by entity/DTO type to avoid provider scans on repeated calls.
/// </summary>
public static class ForgeSourceGeneratedRegistry
{
    private static readonly List<IForgeSourceGeneratedAccessorProvider> Providers = new();
    private static readonly ConcurrentDictionary<Type, IForgeSourceGeneratedAccessorProvider?> ProviderByType = new();
    private static readonly object Gate = new();

    public static ForgeOrmCompilationMode CompilationMode { get; set; } = ForgeOrmCompilationMode.Auto;

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
        provider = ProviderByType.GetOrAdd(type, static t =>
        {
            lock (Gate)
                return Providers.FirstOrDefault(x => x.CanHandle(t));
        })!;

        return provider is not null;
    }
}
