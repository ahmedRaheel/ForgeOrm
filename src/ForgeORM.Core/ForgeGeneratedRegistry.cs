using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

/// <summary>
/// Runtime registry used by ForgeORM source-generated materializers and parameter binders.
/// Source generators can register delegates through a ModuleInitializer so the runtime
/// uses generated code first, then MSIL emit, then safe fallback paths.
/// </summary>
public static class ForgeGeneratedRegistry
{
    private static readonly ConcurrentDictionary<Type, Delegate> Readers = new();
    private static readonly ConcurrentDictionary<Type, Delegate> SqlServerReaders = new();
    private static readonly ConcurrentDictionary<Type, Delegate> ParameterBinders = new();
    private static readonly ConcurrentDictionary<Type, Delegate> ReaderFactories = new();
    private static readonly ConcurrentDictionary<Type, Delegate> SqlServerReaderFactories = new();

    /// <summary>Registers a generated reader for an entity or DTO type.</summary>
    public static void RegisterReader<T>(Func<DbDataReader, T> reader)
        where T : notnull
    {
        Readers[typeof(T)] = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>Registers a generated SQL Server direct reader for an entity or DTO type.</summary>
    public static void RegisterSqlServerReader<T>(Func<SqlDataReader, T> reader)
        where T : notnull
    {
        SqlServerReaders[typeof(T)] = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>Registers a generated reader factory. The factory binds ordinals once from the live reader and returns the hot row delegate.</summary>
    public static void RegisterReaderFactory<T>(Func<DbDataReader, Func<DbDataReader, T>> factory)
        where T : notnull
    {
        ReaderFactories[typeof(T)] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>Registers a generated SQL Server direct reader factory. The factory binds ordinals once from the live SqlDataReader.</summary>
    public static void RegisterSqlServerReaderFactory<T>(Func<SqlDataReader, Func<SqlDataReader, T>> factory)
        where T : notnull
    {
        SqlServerReaderFactories[typeof(T)] = factory ?? throw new ArgumentNullException(nameof(factory));
    }


    /// <summary>Registers a generated parameter binder for an anonymous-like request DTO or entity.</summary>
    public static void RegisterParameterBinder<T>(Action<DbCommand, T> binder)
        where T : notnull
    {
        if (binder is null) throw new ArgumentNullException(nameof(binder));
        ParameterBinders[typeof(T)] = new Action<DbCommand, object>((command, value) => binder(command, (T)value));
    }


    /// <summary>Creates a generated reader from a reader-shape-aware factory when available.</summary>
    public static bool TryCreateReader<T>(DbDataReader shapeReader, out Func<DbDataReader, T> reader)
    {
        if (ReaderFactories.TryGetValue(typeof(T), out var value) &&
            value is Func<DbDataReader, Func<DbDataReader, T>> factory)
        {
            reader = factory(shapeReader);
            return true;
        }

        return TryGetReader(out reader);
    }

    /// <summary>Creates a generated SQL Server direct reader from a reader-shape-aware factory when available.</summary>
    public static bool TryCreateSqlServerReader<T>(SqlDataReader shapeReader, out Func<SqlDataReader, T> reader)
    {
        if (SqlServerReaderFactories.TryGetValue(typeof(T), out var value) &&
            value is Func<SqlDataReader, Func<SqlDataReader, T>> factory)
        {
            reader = factory(shapeReader);
            return true;
        }

        return TryGetSqlServerReader(out reader);
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

    /// <summary>Gets a generated SQL Server direct reader when one was emitted by ForgeORM.SourceGenerators.</summary>
    public static bool TryGetSqlServerReader<T>(out Func<SqlDataReader, T> reader)
    {
        if (SqlServerReaders.TryGetValue(typeof(T), out var value) && value is Func<SqlDataReader, T> typed)
        {
            reader = typed;
            return true;
        }

        reader = default!;
        return false;
    }

    /// <summary>Gets a generated object reader when the runtime only has a Type.</summary>
    public static bool TryGetObjectReader(Type type, out Func<DbDataReader, object> reader)
    {
        if (Readers.TryGetValue(type, out var value))
        {
            reader = r => value.DynamicInvoke(r)!;
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
