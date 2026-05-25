using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

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
    public async ValueTask InsertAsync(DbConnection connection, DbTransaction transaction, TDto dto, object parentKey, CancellationToken cancellationToken)
    {
        var rows = _selector(dto)?.ToList() ?? [];
        if (rows.Count == 0) return;

        var entities = rows.Select(x => ForgeObjectMapper.Map<TChildEntity>(x!)).ToList();
        if (_foreignKey is not null)
        {
            foreach (var entity in entities)
                ForgeRuntimeAccessorCache.Set(_foreignKey, entity!, ForgeObjectMapper.ConvertTo(parentKey, _foreignKey.PropertyType));
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

    private async ValueTask ExecuteSqlServerTvpAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<TChildEntity> entities, CancellationToken cancellationToken)
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

    private static async ValueTask ExecuteSqlServerOpenJsonAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<TChildEntity> entities, CancellationToken cancellationToken)
    {
        // Safe fallback until provider-specific OPENJSON SQL generation is enabled.
        // This keeps the public API compile-safe and behaviorally correct.
        await ExecuteRowByRowFallbackAsync(connection, transaction, entities, cancellationToken);
    }

    private static async ValueTask ExecuteRowByRowFallbackAsync(DbConnection connection, DbTransaction transaction, IReadOnlyList<TChildEntity> entities, CancellationToken cancellationToken)
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
