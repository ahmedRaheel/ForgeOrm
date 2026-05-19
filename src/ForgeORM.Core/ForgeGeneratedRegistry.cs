using System.Collections.Concurrent;
using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Runtime registry used by ForgeORM source-generated materializers and parameter binders.
/// Source generators can register delegates through a ModuleInitializer so the runtime
/// uses generated code first, then MSIL emit, then safe fallback paths.
/// </summary>
public static class ForgeGeneratedRegistry
{
    private static readonly ConcurrentDictionary<Type, Delegate> Readers = new();
    private static readonly ConcurrentDictionary<Type, Delegate> ParameterBinders = new();

    /// <summary>Registers a generated reader for an entity or DTO type.</summary>
    public static void RegisterReader<T>(Func<DbDataReader, T> reader)
        where T : notnull
    {
        Readers[typeof(T)] = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>Registers a generated parameter binder for an anonymous-like request DTO or entity.</summary>
    public static void RegisterParameterBinder<T>(Action<DbCommand, T> binder)
        where T : notnull
    {
        if (binder is null) throw new ArgumentNullException(nameof(binder));
        ParameterBinders[typeof(T)] = new Action<DbCommand, object>((command, value) => binder(command, (T)value));
    }

    /// <summary>Gets a generated reader when one was emitted by ForgeORM.SourceGenerators.</summary>
    public static bool TryGetReader<T>(out Func<DbDataReader, T> reader)
    {
        if (Readers.TryGetValue(typeof(T), out var value) && value is Func<DbDataReader, T> typed)
        {
            reader = typed;
            return true;
        }

        reader = default!;
        return false;
    }

    /// <summary>Gets a generated parameter binder when available.</summary>
    public static bool TryGetParameterBinder(Type parameterType, out Action<DbCommand, object> binder)
    {
        if (ParameterBinders.TryGetValue(parameterType, out var value) && value is Action<DbCommand, object> typed)
        {
            binder = typed;
            return true;
        }

        binder = default!;
        return false;
    }
}
