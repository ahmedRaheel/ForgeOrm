using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Selects how ForgeORM obtains compiled readers, binders and accessors.
/// Auto prefers source-generated code when registered and falls back to RuntimeEmit.
/// </summary>
public enum ForgeOrmCompilationMode
{
    Auto = 0,
    RuntimeEmit = 1,
    SourceGenerated = 2
}

public interface IForgeSourceGeneratedAccessorProvider
{
    bool CanHandle(Type type);
    Func<DbDataReader, object> GetReader(Type type, DbDataReader reader);
    Action<DbCommand, object> GetBinder(Type type);
}

public static class ForgeSourceGeneratedRegistry
{
    private static readonly List<IForgeSourceGeneratedAccessorProvider> Providers = new();
    private static readonly object Gate = new();

    public static ForgeOrmCompilationMode CompilationMode { get; set; } = ForgeOrmCompilationMode.Auto;

    public static void Register(IForgeSourceGeneratedAccessorProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (Gate)
            Providers.Add(provider);
    }

    public static bool TryGetProvider(Type type, out IForgeSourceGeneratedAccessorProvider provider)
    {
        lock (Gate)
        {
            provider = Providers.FirstOrDefault(x => x.CanHandle(type))!;
            return provider is not null;
        }
    }
}
