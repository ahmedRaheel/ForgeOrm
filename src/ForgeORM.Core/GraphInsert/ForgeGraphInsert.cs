using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Executes the TDto operation.
    /// </summary>
    /// <typeparam name="TEntity">The type used by the operation.</typeparam>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TDto operation.</returns>
    public Task<int> InsertAsync<TEntity, TDto>(TDto dto, CancellationToken cancellationToken = default)
        where TEntity : new()
    {
        var entity = ForgeObjectMapper.Map<TEntity>(dto!);
        return InsertAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="configure">The configure value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public async Task<TKey> InsertGraphAsync<TParent, TDto, TKey>(
        TDto dto,
        Action<ForgeGraphInsertOptions<TParent, TDto>> configure,
        CancellationToken cancellationToken = default)
        where TParent : new()
    {
        var options = new ForgeGraphInsertOptions<TParent, TDto>();
        configure(options);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var parent = ForgeObjectMapper.Map<TParent>(dto!);
            var parentKey = await InsertParentAndReturnKeyAsync<TParent, TDto, TKey>(connection, transaction, parent, options, cancellationToken);

            if (options.IncludeChildren)
            {
                foreach (var child in options.ChildMappings)
                {
                    await child.InsertAsync(connection, transaction, dto!, parentKey!, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return parentKey;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Inserts the parent entity from a DTO using graph runtime options. This overload is useful
    /// for parent-only inserts or when samples want to demonstrate strategy selection without
    /// explicit child mapping. Use the graph-mapping overload for parent + children.
    /// </summary>
    public async Task<TKey> InsertGraphAsync<TParent, TDto, TKey>(
        TDto dto,
        Action<ForgeGraphOptions> configure,
        CancellationToken cancellationToken = default)
        where TParent : new()
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ForgeGraphOptions();
        configure(options);

        var graphOptions = new ForgeGraphInsertOptions<TParent, TDto>
        {
            IncludeChildren = false,
            UseBulkWhenPossible = options.UseBulkWhenPossible,
            BatchSize = options.BatchSize,
            Strategy = options.Strategy
        };

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var parent = ForgeObjectMapper.Map<TParent>(dto!);
            var parentKey = await InsertParentAndReturnKeyAsync<TParent, TDto, TKey>(
                connection,
                transaction,
                parent,
                graphOptions,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return parentKey;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes the TDto operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="configure">The configure value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TDto operation.</returns>
    public async Task<object?> InsertGraphAsync<TParent, TDto>(
        TDto dto,
        Action<ForgeGraphInsertOptions<TParent, TDto>> configure,
        CancellationToken cancellationToken = default)
        where TParent : new()
    {
        var key = await InsertGraphAsync<TParent, TDto, object>(dto, configure, cancellationToken);
        return key;
    }


    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="parent">The parent value.</param>
    /// <param name="children">The children value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public async Task<TKey> InsertGraphAsync<TParent, TChild, TKey>(
        TParent parent,
        Expression<Func<TParent, IEnumerable<TChild>>> children,
        Expression<Func<TParent, TKey>> parentKey,
        Expression<Func<TChild, TKey>> childForeignKey,
        CancellationToken cancellationToken = default)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));

        var parentKeyProperty = ForgeExpression.Property(parentKey.Body);
        var childForeignKeyProperty = ForgeExpression.Property(childForeignKey.Body);
        var childAccessor = children.Compile();
        var childRows = childAccessor(parent)?.ToList() ?? [];

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var options = new ForgeGraphInsertOptions<TParent, TParent>();
            options.ParentOptions.KeyProperty = parentKeyProperty;
            var key = await InsertParentAndReturnKeyAsync<TParent, TParent, TKey>(
                connection,
                transaction,
                parent,
                options,
                cancellationToken);

            foreach (var child in childRows)
            {
                if (child is null) continue;
                if (childForeignKeyProperty.CanWrite)
                    childForeignKeyProperty.SetValue(child, ForgeObjectMapper.ConvertTo(key, childForeignKeyProperty.PropertyType));

                ForgeEntityShape.EnsureGeneratedKey(child);
                ResetDatabaseGeneratedIdentity(child);
            }

            await InsertChildrenRowByRowAsync(connection, transaction, childRows, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return key;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TChildDto">The type used by the operation.</typeparam>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="parentFactory">The parentFactory value.</param>
    /// <param name="children">The children value.</param>
    /// <param name="childFactory">The childFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public Task<TKey> InsertGraphAsync<TDto, TParent, TChildDto, TChild, TKey>(
        TDto dto,
        Func<TDto, TParent> parentFactory,
        Func<TDto, IEnumerable<TChildDto>> children,
        Func<TParent, TChildDto, TChild> childFactory,
        Expression<Func<TParent, TKey>> parentKey,
        Expression<Func<TChild, TKey>> childForeignKey,
        CancellationToken cancellationToken = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (parentFactory is null) throw new ArgumentNullException(nameof(parentFactory));
        if (children is null) throw new ArgumentNullException(nameof(children));
        if (childFactory is null) throw new ArgumentNullException(nameof(childFactory));

        var parent = parentFactory(dto);
        var childRows = children(dto).Select(child => childFactory(parent, child)).ToList();
        return InsertGraphAsync(
            parent,
            _ => childRows,
            parentKey,
            childForeignKey,
            cancellationToken);
    }

    private static async Task InsertChildrenRowByRowAsync<TChild>(
        DbConnection connection,
        DbTransaction transaction,
        IReadOnlyList<TChild> children,
        CancellationToken cancellationToken)
    {
        if (children.Count == 0) return;

        var shape = ForgeEntityShape.For(typeof(TChild));
        var key = shape.KeyProperty;
        var props = ForgeGraphWriteHelpers.GetInsertProperties(shape, includeKey: false);
        var sql = ForgeGraphWriteHelpers.BuildInsertSql(shape, props, includeScopeIdentity: key is not null);

        foreach (var child in children)
        {
            if (child is null) continue;
            ResetDatabaseGeneratedIdentity(child!);
            var parameters = ForgeGraphWriteHelpers.CreateParameterDictionary(props, child!);
            await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction);
            if (key is not null)
            {
                var generated = await command.ExecuteScalarAsync(cancellationToken);
                if (key.CanWrite)
                    key.SetValue(child!, ForgeObjectMapper.ConvertTo(generated, key.PropertyType));
            }
            else
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    private static void ResetDatabaseGeneratedIdentity(object entity)
    {
        var shape = ForgeEntityShape.For(entity.GetType());
        var key = shape.KeyProperty;
        if (key is null || !key.CanWrite)
            return;

        var keyType = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        if (keyType == typeof(Guid))
            return;

        if (keyType == typeof(int) || keyType == typeof(long) || keyType == typeof(short))
            key.SetValue(entity, Activator.CreateInstance(keyType));
    }

    private static async Task<TKey> InsertParentAndReturnKeyAsync<TParent, TDto, TKey>(
        DbConnection connection,
        DbTransaction transaction,
        TParent parent,
        ForgeGraphInsertOptions<TParent, TDto> options,
        CancellationToken cancellationToken)
    {
        var entity = ForgeEntityShape.For(typeof(TParent));
        var key = options.ParentOptions.KeyProperty ?? entity.KeyProperty;
        if (key is null)
            throw new InvalidOperationException($"ForgeORM graph insert requires a key property on {typeof(TParent).Name}. Use graph.Parent().Key(x => x.Id).");

        EnsureKeyValue(parent!, key);
        ResetDatabaseGeneratedIdentity(parent!);

        var keyValue = key.GetValue(parent);
        var includeKeyInInsert = ShouldIncludeKeyInInsert(key, keyValue);
        var props = ForgeGraphWriteHelpers.GetInsertProperties(entity, includeKeyInInsert);
        var sql = ForgeGraphWriteHelpers.BuildInsertSql(entity, props, includeScopeIdentity: !includeKeyInInsert);
        var parameters = ForgeGraphWriteHelpers.CreateParameterDictionary(props, parent!);
        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction);
        var result = includeKeyInInsert ? keyValue : await command.ExecuteScalarAsync(cancellationToken);
        if (includeKeyInInsert)
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (key.CanWrite)
        {
            key.SetValue(parent, ForgeObjectMapper.ConvertTo(result, key.PropertyType));
        }

        return (TKey)ForgeObjectMapper.ConvertTo(result, typeof(TKey))!;
    }

    private static bool ShouldIncludeKeyInInsert(PropertyInfo key, object? value)
    {
        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        if (type == typeof(Guid)) return true;
        if (value is null) return false;
        if (Equals(value, Activator.CreateInstance(type))) return false;
        return type != typeof(int) && type != typeof(long) && type != typeof(short);
    }

    private static void EnsureKeyValue(object entity, PropertyInfo key)
    {
        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        var current = key.GetValue(entity);
        if (type == typeof(Guid) && key.CanWrite && (current is null || (Guid)current == Guid.Empty))
            key.SetValue(entity, Guid.NewGuid());
    }

    private static bool SameProperty(PropertyInfo a, PropertyInfo b)
        => a.Name.Equals(b.Name, StringComparison.OrdinalIgnoreCase);
}

public sealed class ForgeGraphInsertOptions<TParent, TDto>
{
    internal ForgeGraphParentOptions<TParent> ParentOptions { get; } = new();
    internal List<IForgeGraphChildInsert<TDto>> ChildMappings { get; } = [];

    /// <summary>Gets or sets whether child mappings should be inserted.</summary>
    public bool IncludeChildren { get; set; } = true;

    /// <summary>Gets or sets whether bulk strategies may be used for children.</summary>
    public bool UseBulkWhenPossible { get; set; } = true;

    /// <summary>Gets or sets the preferred batch size for child inserts.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Gets or sets the preferred child insert strategy.</summary>
    public ForgeBulkStrategy Strategy { get; set; } = ForgeBulkStrategy.Auto;

    /// <summary>
    /// Executes the Parent operation.
    /// </summary>
    /// <returns>The result of the Parent operation.</returns>
    public ForgeGraphParentOptions<TParent> Parent() => ParentOptions;

    /// <summary>
    /// Executes the TChildDto operation.
    /// </summary>
    /// <typeparam name="TChildEntity">The type used by the operation.</typeparam>
    /// <typeparam name="TChildDto">The type used by the operation.</typeparam>
    /// <param name="selector">The selector value.</param>
    /// <returns>The result of the TChildDto operation.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> ChildrenOf<TChildEntity, TChildDto>(
        Expression<Func<TDto, IEnumerable<TChildDto>>> selector)
        where TChildEntity : new()
    {
        var options = new ForgeGraphChildOptions<TDto, TChildEntity, TChildDto>(selector.Compile());
        ChildMappings.Add(options);
        return options;
    }

    /// <summary>
    /// Executes the TChildDto operation.
    /// </summary>
    /// <typeparam name="TChildEntity">The type used by the operation.</typeparam>
    /// <typeparam name="TChildDto">The type used by the operation.</typeparam>
    /// <param name="selector">The selector value.</param>
    /// <returns>The result of the TChildDto operation.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> Children<TChildEntity, TChildDto>(
        Expression<Func<TDto, IEnumerable<TChildDto>>> selector)
        where TChildEntity : new()
        => ChildrenOf<TChildEntity, TChildDto>(selector);
}

public sealed class ForgeGraphParentOptions<TParent>
{
    internal PropertyInfo? KeyProperty { get; set; }

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public ForgeGraphParentOptions<TParent> Key<TKey>(Expression<Func<TParent, TKey>> key)
    {
        KeyProperty = ForgeExpression.Property(key.Body);
        return this;
    }
}

public sealed class ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> : IForgeGraphChildInsert<TDto>
    where TChildEntity : new()
{
    private readonly Func<TDto, IEnumerable<TChildDto>> _selector;
    private PropertyInfo? _foreignKey;
    private string? _tableType;
    private string? _procedure;
    private string _parameterName = "@Items";
    private bool _useOpenJson;

    internal ForgeGraphChildOptions(Func<TDto, IEnumerable<TChildDto>> selector) => _selector = selector;

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="foreignKey">The foreignKey value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> ForeignKey<TKey>(Expression<Func<TChildEntity, TKey>> foreignKey)
    {
        _foreignKey = ForgeExpression.Property(foreignKey.Body);
        return this;
    }

    /// <summary>
    /// Executes the UseSqlServerTvp operation.
    /// </summary>
    /// <param name="tableType">The tableType value.</param>
    /// <param name="procedure">The procedure value.</param>
    /// <param name="parameterName">The parameterName value.</param>
    /// <returns>The result of the UseSqlServerTvp operation.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> UseSqlServerTvp(
        string tableType,
        string procedure,
        string parameterName = "@Items")
    {
        _tableType = tableType;
        _procedure = procedure;
        _parameterName = parameterName.StartsWith('@') ? parameterName : "@" + parameterName;
        return this;
    }

    /// <summary>
    /// Uses SQL Server OPENJSON for child insertion.
    /// Current implementation falls back safely to row-by-row when provider JSON generation is not available.
    /// </summary>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> UseSqlServerOpenJson()
    {
        _useOpenJson = true;
        return this;
    }

    /// <summary>
    /// Executes the InsertAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="dto">The dto value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the InsertAsync operation.</returns>
    public async Task InsertAsync(DbConnection connection, DbTransaction transaction, TDto dto, object parentKey, CancellationToken cancellationToken)
    {
        var rows = _selector(dto)?.ToList() ?? [];
        if (rows.Count == 0) return;

        var entities = rows.Select(x => ForgeObjectMapper.Map<TChildEntity>(x!)).ToList();
        if (_foreignKey is not null)
        {
            foreach (var entity in entities)
                _foreignKey.SetValue(entity, ForgeObjectMapper.ConvertTo(parentKey, _foreignKey.PropertyType));
        }

        foreach (var entity in entities)
            ForgeEntityShape.EnsureGeneratedKey(entity!);

        if (_useOpenJson)
        {
            await ExecuteSqlServerOpenJsonAsync(connection, transaction, entities, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_tableType) && !string.IsNullOrWhiteSpace(_procedure))
        {
            await ExecuteSqlServerTvpAsync(connection, transaction, entities, cancellationToken);
            return;
        }

        await ExecuteRowByRowFallbackAsync(connection, transaction, entities, cancellationToken);
    }

    private async Task ExecuteSqlServerTvpAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<TChildEntity> entities, CancellationToken cancellationToken)
    {
        var table = ForgeTvpDataTable.Create(entities);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = _procedure!;
        command.CommandType = CommandType.StoredProcedure;

        var parameter = command.CreateParameter();
        parameter.ParameterName = _parameterName;
        parameter.Value = table;
        ForgeSqlServerParameterConfigurator.ConfigureStructured(parameter, _tableType!);
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteSqlServerOpenJsonAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<TChildEntity> entities, CancellationToken cancellationToken)
    {
        // Safe fallback until provider-specific OPENJSON SQL generation is enabled.
        // This keeps the public API compile-safe and behaviorally correct.
        await ExecuteRowByRowFallbackAsync(connection, transaction, entities, cancellationToken);
    }

    private static async Task ExecuteRowByRowFallbackAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<TChildEntity> entities, CancellationToken cancellationToken)
    {
        var shape = ForgeEntityShape.For(typeof(TChildEntity));
        var key = shape.KeyProperty;
        var props = ForgeGraphWriteHelpers.GetInsertProperties(shape, includeKey: false);
        var sql = ForgeGraphWriteHelpers.BuildInsertSql(shape, props, includeScopeIdentity: key is not null);
        foreach (var entity in entities)
        {
            if (entity is null)
                continue;

            ForgeGraphWriteHelpers.ResetDatabaseGeneratedIdentity(entity!);

            var parameters =
                ForgeGraphWriteHelpers.CreateParameterDictionary(
                    props,
                    entity!);

            await using var command =
                ForgeAdo.CreateCommand(
                    connection,
                    sql,
                    parameters,
                    transaction);

            if (key is not null)
            {
                var generated =
                    await command.ExecuteScalarAsync(cancellationToken);

                ForgeGraphWriteHelpers.SetDatabaseGeneratedIdentity(
                    entity!,
                    generated);
            }
            else
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}

internal interface IForgeGraphChildInsert<TDto>
{
    /// <summary>
    /// Defines the InsertAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="dto">The dto value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the InsertAsync operation.</returns>
    Task InsertAsync(DbConnection connection, DbTransaction transaction, TDto dto, object parentKey, CancellationToken cancellationToken);
}
internal static partial class ForgeGraphWriteHelpers
{
    internal static void ResetDatabaseGeneratedIdentity(object entity)
    {
        if (entity is null)
            return;

        var identity = entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (identity is null)
            return;

        if (!identity.CanWrite)
            return;

        var type = Nullable.GetUnderlyingType(identity.PropertyType)
                   ?? identity.PropertyType;

        object? defaultValue = type.IsValueType
            ? Activator.CreateInstance(type)
            : null;

        identity.SetValue(entity, defaultValue);
    }

    internal static void SetDatabaseGeneratedIdentity(
        object entity,
        object? generatedId)
    {
        if (entity is null)
            return;

        if (generatedId is null || generatedId == DBNull.Value)
            return;

        var identity = entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (identity is null)
            return;

        if (!identity.CanWrite)
            return;

        var targetType =
            Nullable.GetUnderlyingType(identity.PropertyType)
            ?? identity.PropertyType;

        var converted =
            Convert.ChangeType(generatedId, targetType);

        identity.SetValue(entity, converted);
    }
}
internal static class ForgeEnumConversion
{
    /// <summary>
    /// Executes the StorageType operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the StorageType operation.</returns>
    public static Type StorageType(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (!type.IsEnum) return type;

        var attr = property.GetCustomAttribute<ForgeEnumStorageAttribute>();
        return attr?.Storage == ForgeEnumStorage.Number
            ? Enum.GetUnderlyingType(type)
            : typeof(string);
    }

    /// <summary>
    /// Executes the ToDatabaseValue operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the ToDatabaseValue operation.</returns>
    public static object? ToDatabaseValue(object? value, PropertyInfo? property = null)
    {
        if (value is null || value is DBNull) return value;

        var type = value.GetType();
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (!enumType.IsEnum) return value;

        var storage = property?.GetCustomAttribute<ForgeEnumStorageAttribute>()?.Storage ?? ForgeEnumStorage.Number;
        return storage == ForgeEnumStorage.Number
            ? Convert.ChangeType(value, Enum.GetUnderlyingType(enumType))
            : value.ToString();
    }

    /// <summary>
    /// Executes the ToEnumOrValue operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="targetType">The targetType value.</param>
    /// <returns>The result of the ToEnumOrValue operation.</returns>
    public static object? ToEnumOrValue(object? value, Type targetType)
    {
        if (value is null || value is DBNull)
        {
            var nullable = Nullable.GetUnderlyingType(targetType);
            if (nullable is not null || !targetType.IsValueType) return null;
            return Activator.CreateInstance(targetType);
        }

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (!type.IsEnum) return null;
        if (value is string text) return Enum.Parse(type, text, ignoreCase: true);
        return Enum.ToObject(type, value);
    }
}

internal static class ForgeTvpDataTable
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public static DataTable Create<T>(IReadOnlyList<T> rows)
    {
        var shape = ForgeEntityShape.For(typeof(T));
        var props = ForgeGraphWriteHelpers.GetInsertProperties(shape);
        var table = new DataTable();

        foreach (var prop in props)
            table.Columns.Add(ForgeEntityShape.ColumnName(prop), ForgeEnumConversion.StorageType(prop));

        foreach (var row in rows)
        {
            var values = props.Select(p => ForgeGraphWriteHelpers.NormalizeDatabaseValue(p.GetValue(row), p) ?? DBNull.Value).ToArray();
            table.Rows.Add(values);
        }

        return table;
    }
}


internal static partial class ForgeGraphWriteHelpers
{
    public static List<PropertyInfo> GetInsertProperties(ForgeEntityShape shape, bool includeKey = false)
    {
        var key = shape.KeyProperty;

        return shape.ScalarProperties
            .Where(p => p.CanRead)
            .Where(p => !ForgeEntityShape.IsComputed(p))
            // Numeric identity keys are database generated and must never be sent in INSERT.
            // This prevents: Cannot insert explicit value for identity column ... IDENTITY_INSERT is OFF.
            .Where(p => key is null || !p.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase) || (includeKey && !IsDatabaseGeneratedIdentityKey(key)))
            .ToList();
    }

    private static bool IsDatabaseGeneratedIdentityKey(PropertyInfo key)
    {
        var keyType = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        return keyType == typeof(int)
            || keyType == typeof(long)
            || keyType == typeof(short);
    }

    public static List<PropertyInfo> GetUpdateProperties(ForgeEntityShape shape)
    {
        var key = shape.KeyProperty;

        return shape.ScalarProperties
            .Where(p => p.CanRead)
            .Where(p => !ForgeEntityShape.IsComputed(p))
            .Where(p => key is null || !p.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static string BuildInsertSql(ForgeEntityShape shape, IReadOnlyList<PropertyInfo> props, bool includeScopeIdentity)
    {
        var key = shape.KeyProperty;
        var safeProps = props
            .Where(p => p.CanRead)
            .Where(p => key is null || !p.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase) || !IsDatabaseGeneratedIdentityKey(key))
            .ToArray();

        if (safeProps.Length == 0)
            throw new InvalidOperationException($"No insertable scalar columns were found for table {shape.TableName}.");

        var columns = string.Join(", ", safeProps.Select(ForgeEntityShape.ColumnName));
        var values = string.Join(", ", safeProps.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {shape.TableName} ({columns}) VALUES ({values})";

        if (includeScopeIdentity && key is not null && IsDatabaseGeneratedIdentityKey(key))
            sql += "; SELECT CAST(SCOPE_IDENTITY() AS int);";

        return sql;
    }

    public static Dictionary<string, object?> CreateParameterDictionary(IEnumerable<PropertyInfo> props, object entity)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in props)
            parameters[prop.Name] = NormalizeDatabaseValue(prop.GetValue(entity), prop);

        return parameters;
    }

    public static object? NormalizeDatabaseValue(object? value, PropertyInfo? property = null)
    {
        value = ForgeEnumConversion.ToDatabaseValue(value, property);

        if (value is DateTime dateTime)
        {
            if (dateTime == default || dateTime < new DateTime(1753, 1, 1))
                return DateTime.UtcNow;

            return dateTime;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            if (dateTimeOffset == default)
                return DateTimeOffset.UtcNow;

            return dateTimeOffset;
        }

        return value;
    }
}

internal static class ForgeSqlServerParameterConfigurator
{
    /// <summary>
    /// Executes the ConfigureStructured operation.
    /// </summary>
    /// <param name="parameter">The parameter value.</param>
    /// <param name="tableType">The tableType value.</param>
    public static void ConfigureStructured(DbParameter parameter, string tableType)
    {
        var type = parameter.GetType();
        type.GetProperty("TypeName")?.SetValue(parameter, tableType);

        var sqlDbType = type.GetProperty("SqlDbType");
        if (sqlDbType is not null)
        {
            var structured = Enum.Parse(sqlDbType.PropertyType, "Structured");
            sqlDbType.SetValue(parameter, structured);
        }
    }
}

internal static class ForgeObjectMapper
{
    /// <summary>
    /// Executes the TTarget operation.
    /// </summary>
    /// <typeparam name="TTarget">The type used by the operation.</typeparam>
    /// <param name="new">The new value.</param>
    /// <returns>The result of the TTarget operation.</returns>
    public static TTarget Map<TTarget>(object source) where TTarget : new()
    {
        if (source is TTarget typed) return typed;
        var target = new TTarget();
        Copy(source, target);
        return target;
    }

    /// <summary>
    /// Executes the Copy operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="target">The target value.</param>
    public static void Copy(object source, object target)
    {
        var sourceProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var targetProp in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            if (!sourceProps.TryGetValue(targetProp.Name, out var sourceProp)) continue;
            if (IsEnumerableButNotString(targetProp.PropertyType)) continue;
            var value = sourceProp.GetValue(source);
            targetProp.SetValue(target, ConvertTo(value, targetProp.PropertyType));
        }
    }

    /// <summary>
    /// Executes the ConvertTo operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="targetType">The targetType value.</param>
    /// <returns>The result of the ConvertTo operation.</returns>
    public static object? ConvertTo(object? value, Type targetType)
    {
        if (value is null || value is DBNull)
        {
            var nullable = Nullable.GetUnderlyingType(targetType);
            if (nullable is not null || !targetType.IsValueType) return null;
            return Activator.CreateInstance(targetType);
        }

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type.IsEnum)
        {
            if (value is string text)
                return Enum.Parse(type, text, ignoreCase: true);
            return Enum.ToObject(type, value);
        }
        if (type == typeof(Guid)) return value is Guid g ? g : Guid.Parse(value.ToString()!);
        if (type == typeof(DateTimeOffset)) return value is DateTimeOffset dto ? dto : new DateTimeOffset(Convert.ToDateTime(value));
        if (type.IsAssignableFrom(value.GetType())) return value;
        return Convert.ChangeType(value, type);
    }

    private static bool IsScalarColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(byte[]);
    }

    private static bool IsEnumerableButNotString(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}

internal sealed class ForgeEntityShape
{
    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public required string TableName { get; init; }
    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public required PropertyInfo? KeyProperty { get; init; }
    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public required IReadOnlyList<PropertyInfo> ScalarProperties { get; init; }

    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public static ForgeEntityShape For(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalarColumnType(p.PropertyType))
            .ToList();

        return new ForgeEntityShape
        {
            TableName = ResolveTableName(type),
            KeyProperty = props.FirstOrDefault(p => p.GetCustomAttribute<ForgeKeyAttribute>() is not null)
                ?? props.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)),
            ScalarProperties = props
        };
    }

    /// <summary>
    /// Executes the ResolveTableName operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the ResolveTableName operation.</returns>
    public static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttribute<ForgeTableAttribute>();
        if (attr is not null) return attr.Name;
        return type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s";
    }

    /// <summary>
    /// Executes the ColumnName operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the ColumnName operation.</returns>
    public static string ColumnName(PropertyInfo property)
        => property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;

    /// <summary>
    /// Executes the IsComputed operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the IsComputed operation.</returns>
    public static bool IsComputed(PropertyInfo property)
        => property.GetCustomAttribute<ForgeComputedAttribute>() is not null;

    /// <summary>
    /// Executes the EnsureGeneratedKey operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    public static void EnsureGeneratedKey(object entity)
    {
        var shape = For(entity.GetType());
        var key = shape.KeyProperty;
        if (key is null || !key.CanWrite) return;
        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        var current = key.GetValue(entity);
        if (type == typeof(Guid) && (current is null || (Guid)current == Guid.Empty))
            key.SetValue(entity, Guid.NewGuid());
    }

    private static bool IsScalarColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(byte[]);
    }

    private static bool IsEnumerableButNotString(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}

internal static class ForgeExpression
{
    /// <summary>
    /// Executes the Property operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the Property operation.</returns>
    public static PropertyInfo Property(Expression expression)
    {
        while (expression is UnaryExpression unary &&
               (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked || unary.NodeType == ExpressionType.TypeAs))
            expression = unary.Operand;

        if (expression is MemberExpression { Member: PropertyInfo property })
            return property;

        throw new NotSupportedException("Expression must point to a property.");
    }
}
