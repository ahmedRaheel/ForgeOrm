using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>Selects the materializer compilation strategy. Source-generation values are retained only for binary/source compatibility and map to RuntimeEmit.</summary>
public enum ForgeOrmCompilationMode
{
    /// <summary>Default mode; currently maps to RuntimeEmit.</summary>
    Auto = 0,

    /// <summary>MSIL DynamicMethod materializers and compiled binders.</summary>
    RuntimeEmit = 1,

    /// <summary>Legacy compatibility value. Source generator support was removed; this maps to RuntimeEmit.</summary>
    [Obsolete("Source-generation mode was removed. Use RuntimeEmit or Auto.")]
    SourceGenerated = 2,

    /// <summary>Legacy compatibility value. Source generator support was removed; this maps to RuntimeEmit.</summary>
    [Obsolete("Source-generation mode was removed. Use RuntimeEmit or Auto.")]
    SourceGeneratedStrict = 3
}

/// <summary>Runtime compilation state. ForgeORM now uses MSIL RuntimeEmit only.</summary>
public static class ForgeOrmCompilationRuntime
{
    public static ForgeOrmCompilationMode Mode { get; set; } = ForgeOrmCompilationMode.RuntimeEmit;
}

/// <summary>Optional provider-specific typed materializer hook.</summary>
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
