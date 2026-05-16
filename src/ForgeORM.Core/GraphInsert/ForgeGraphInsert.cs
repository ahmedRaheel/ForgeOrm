using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Initializes or executes the TDto> operation.
    /// </summary>
    /// <param name="dto">The dto value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<int> InsertAsync<TEntity, TDto>(TDto dto, CancellationToken cancellationToken = default)
        where TEntity : new()
    {
        var entity = ForgeObjectMapper.Map<TEntity>(dto!);
        return InsertAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the TKey> operation.
    /// </summary>
    /// <param name="dto">The dto value.</param>
    /// <param name="configure">The configure value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
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

            foreach (var child in options.ChildMappings)
                await child.InsertAsync(connection, transaction, dto!, parentKey!, cancellationToken);

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
    /// Initializes or executes the TDto> operation.
    /// </summary>
    /// <param name="dto">The dto value.</param>
    /// <param name="configure">The configure value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<object?> InsertGraphAsync<TParent, TDto>(
        TDto dto,
        Action<ForgeGraphInsertOptions<TParent, TDto>> configure,
        CancellationToken cancellationToken = default)
        where TParent : new()
    {
        var key = await InsertGraphAsync<TParent, TDto, object>(dto, configure, cancellationToken);
        return key;
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

        var keyValue = key.GetValue(parent);
        var includeKeyInInsert = ShouldIncludeKeyInInsert(key, keyValue);
        var props = entity.ScalarProperties
            .Where(p => p.CanRead && !ForgeEntityShape.IsComputed(p) && (includeKeyInInsert || !SameProperty(p, key)))
            .ToList();

        var columns = string.Join(", ", props.Select(ForgeEntityShape.ColumnName));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {entity.TableName} ({columns}) VALUES ({values});";

        if (!includeKeyInInsert)
            sql += " SELECT CAST(SCOPE_IDENTITY() AS int);";

        var parameters = props.ToDictionary(p => p.Name, p => p.GetValue(parent), StringComparer.OrdinalIgnoreCase);
        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction);
        var result = includeKeyInInsert ? keyValue : await command.ExecuteScalarAsync(cancellationToken);
        if (includeKeyInInsert)
            await command.ExecuteNonQueryAsync(cancellationToken);

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

    /// <summary>
    /// Initializes or executes the Parent operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public ForgeGraphParentOptions<TParent> Parent() => ParentOptions;

    /// <summary>
    /// Initializes or executes the TChildDto> operation.
    /// </summary>
    /// <param name="selector">The selector value.</param>
    /// <returns>The operation result.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> ChildrenOf<TChildEntity, TChildDto>(
        Expression<Func<TDto, IEnumerable<TChildDto>>> selector)
        where TChildEntity : new()
    {
        var options = new ForgeGraphChildOptions<TDto, TChildEntity, TChildDto>(selector.Compile());
        ChildMappings.Add(options);
        return options;
    }

    /// <summary>
    /// Initializes or executes the TChildDto> operation.
    /// </summary>
    /// <param name="selector">The selector value.</param>
    /// <returns>The operation result.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> Children<TChildEntity, TChildDto>(
        Expression<Func<TDto, IEnumerable<TChildDto>>> selector)
        where TChildEntity : new()
        => ChildrenOf<TChildEntity, TChildDto>(selector);
}

public sealed class ForgeGraphParentOptions<TParent>
{
    internal PropertyInfo? KeyProperty { get; private set; }

    /// <summary>
    /// Initializes or executes the Key operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <returns>The operation result.</returns>
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

    internal ForgeGraphChildOptions(Func<TDto, IEnumerable<TChildDto>> selector) => _selector = selector;

    /// <summary>
    /// Initializes or executes the ForeignKey operation.
    /// </summary>
    /// <param name="foreignKey">The foreignKey value.</param>
    /// <returns>The operation result.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> ForeignKey<TKey>(Expression<Func<TChildEntity, TKey>> foreignKey)
    {
        _foreignKey = ForgeExpression.Property(foreignKey.Body);
        return this;
    }

    /// <summary>
    /// Initializes or executes the UseSqlServerTvp operation.
    /// </summary>
    /// <param name="tableType">The tableType value.</param>
    /// <param name="procedure">The procedure value.</param>
    /// <param name="parameterName">The parameterName value.</param>
    /// <returns>The operation result.</returns>
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
    /// Initializes or executes the InsertAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="dto">The dto value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
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

    private static async Task ExecuteRowByRowFallbackAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<TChildEntity> entities, CancellationToken cancellationToken)
    {
        var shape = ForgeEntityShape.For(typeof(TChildEntity));
        var props = shape.ScalarProperties.Where(p => p.CanRead && !ForgeEntityShape.IsComputed(p)).ToList();
        var sql = $"INSERT INTO {shape.TableName} ({string.Join(", ", props.Select(ForgeEntityShape.ColumnName))}) VALUES ({string.Join(", ", props.Select(p => "@" + p.Name))})";
        foreach (var entity in entities)
        {
            var parameters = props.ToDictionary(p => p.Name, p => p.GetValue(entity), StringComparer.OrdinalIgnoreCase);
            await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}

internal interface IForgeGraphChildInsert<TDto>
{
    Task InsertAsync(DbConnection connection, DbTransaction transaction, TDto dto, object parentKey, CancellationToken cancellationToken);
}

internal static class ForgeEnumConversion
{
    /// <summary>
    /// Initializes or executes the StorageType operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The operation result.</returns>
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
    /// Initializes or executes the ToDatabaseValue operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="property">The property value.</param>
    /// <returns>The operation result.</returns>
    public static object? ToDatabaseValue(object? value, PropertyInfo? property = null)
    {
        if (value is null || value is DBNull) return value;

        var type = value.GetType();
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (!enumType.IsEnum) return value;

        var storage = property?.GetCustomAttribute<ForgeEnumStorageAttribute>()?.Storage ?? ForgeEnumStorage.String;
        return storage == ForgeEnumStorage.Number
            ? Convert.ChangeType(value, Enum.GetUnderlyingType(enumType))
            : value.ToString();
    }

    /// <summary>
    /// Initializes or executes the ToEnumOrValue operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="targetType">The targetType value.</param>
    /// <returns>The operation result.</returns>
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
    /// Initializes or executes the Create operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The operation result.</returns>
    public static DataTable Create<T>(IReadOnlyList<T> rows)
    {
        var shape = ForgeEntityShape.For(typeof(T));
        var props = shape.ScalarProperties.Where(p => p.CanRead && !ForgeEntityShape.IsComputed(p)).ToList();
        var table = new DataTable();

        foreach (var prop in props)
            table.Columns.Add(ForgeEntityShape.ColumnName(prop), ForgeEnumConversion.StorageType(prop));

        foreach (var row in rows)
        {
            var values = props.Select(p => ForgeEnumConversion.ToDatabaseValue(p.GetValue(row), p) ?? DBNull.Value).ToArray();
            table.Rows.Add(values);
        }

        return table;
    }
}

internal static class ForgeSqlServerParameterConfigurator
{
    /// <summary>
    /// Initializes or executes the ConfigureStructured operation.
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
    /// Initializes or executes the Map operation.
    /// </summary>
    /// <param name="new">The new value.</param>
    /// <returns>The operation result.</returns>
    public static TTarget Map<TTarget>(object source) where TTarget : new()
    {
        if (source is TTarget typed) return typed;
        var target = new TTarget();
        Copy(source, target);
        return target;
    }

    /// <summary>
    /// Initializes or executes the Copy operation.
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
    /// Initializes or executes the ConvertTo operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="targetType">The targetType value.</param>
    /// <returns>The operation result.</returns>
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

    private static bool IsEnumerableButNotString(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}

internal sealed class ForgeEntityShape
{
    public required string TableName { get; init; }
    public required PropertyInfo? KeyProperty { get; init; }
    public required IReadOnlyList<PropertyInfo> ScalarProperties { get; init; }

    /// <summary>
    /// Initializes or executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeEntityShape For(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsEnumerableButNotString(p.PropertyType))
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
    /// Initializes or executes the ResolveTableName operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The operation result.</returns>
    public static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttribute<ForgeTableAttribute>();
        if (attr is not null) return attr.Name;
        return type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s";
    }

    /// <summary>
    /// Initializes or executes the ColumnName operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The operation result.</returns>
    public static string ColumnName(PropertyInfo property)
        => property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;

    /// <summary>
    /// Initializes or executes the IsComputed operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The operation result.</returns>
    public static bool IsComputed(PropertyInfo property)
        => property.GetCustomAttribute<ForgeComputedAttribute>() is not null;

    /// <summary>
    /// Initializes or executes the EnsureGeneratedKey operation.
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

    private static bool IsEnumerableButNotString(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}

internal static class ForgeExpression
{
    /// <summary>
    /// Initializes or executes the Property operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The operation result.</returns>
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
