using System.Collections.Concurrent;
using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Selects how ForgeORM obtains compiled readers, binders and accessors.
/// Auto prefers source-generated code when registered and falls back to RuntimeEmit.
/// NativeAOT users can explicitly select SourceGenerated from configuration.
/// </summary>
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
    Func<DbDataReader, object> GetReader(Type type, DbDataReader reader);
    Action<DbCommand, object> GetBinder(Type type);
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

    public static void Register(IForgeSourceGeneratedAccessorProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (Gate)
        {
            Providers.Add(provider);
            ProviderByType.Clear();
        }
    }

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
