using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeORM.Core;

internal abstract class ForgeBulkExecutorBase : IForgeProviderBulkExecutor
{
    public ValueTask<int> ExecuteManyAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (rows is null || rows.Count == 0)
            return new ValueTask<int>(0);

        var props = ForgeProviderAdo.PropertyCache<T>.Properties;
        if (props.Length == 0)
            return new ValueTask<int>(0);

        return ExecuteCoreAsync(connection, RewriteSql(sql), rows, props, cancellationToken);
    }

    private async ValueTask<int> ExecuteCoreAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        (PropertyInfo Info, string ParamName, Type DeclaredType)[] props,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        var parameters = new DbParameter[props.Length];

        for (var i = 0; i < props.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = FormatParameterName(props[i].Info.Name);
            ApplyProviderParameterSettings(parameter, props[i].DeclaredType);
            command.Parameters.Add(parameter);
            parameters[i] = parameter;
        }

        TryPrepare(command);

        var affected = 0;

        foreach (var row in rows)
        {
            for (var i = 0; i < props.Length; i++)
            {
                var value = ForgeProviderAccessors.Get(props[i].Info, row!);
                parameters[i].Value = NormalizeValue(value, props[i].DeclaredType) ?? DBNull.Value;
            }

            affected += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return affected;
    }

    protected virtual string RewriteSql(string sql) => sql;

    protected virtual string FormatParameterName(string name) => "@" + name;

    protected virtual void ApplyProviderParameterSettings(DbParameter parameter, Type declaredType)
    {
    }

    private static void TryPrepare(DbCommand command)
    {
        try
        {
            command.Prepare();
        }
        catch
        {
            // Some providers do not support Prepare for every SQL shape.
        }
    }

    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null)
            return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
            return value.ToString();

        if (actual == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            return dateTime == default || dateTime < new DateTime(1753, 1, 1)
                ? DateTime.UtcNow
                : dateTime;
        }

        if (actual == typeof(DateTimeOffset))
        {
            var dto = (DateTimeOffset)value;
            return dto == default ? DateTimeOffset.UtcNow : dto;
        }

        return value;
    }
}
