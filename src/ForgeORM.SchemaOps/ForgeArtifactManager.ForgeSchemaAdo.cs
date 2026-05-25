using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.QueryAst.Artifacts;

namespace ForgeORM.SchemaOps;

internal static class ForgeSchemaAdo
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask<IReadOnlyList<T>> QueryAsync<T>(DbConnection connection, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken)) rows.Add(Map<T>(reader));
        return rows;
    }

    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public static async ValueTask<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql, object? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameters is not null)
        {
            foreach (var prop in parameters.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanRead))
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@" + prop.Name;
                parameter.Value = prop.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }
        return command;
    }

    private static T Map<T>(DbDataReader reader)
    {
        var instance = Activator.CreateInstance<T>();
        var props = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanWrite).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!props.TryGetValue(reader.GetName(i), out var prop) || reader.IsDBNull(i)) continue;
            var value = reader.GetValue(i);
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            prop.SetValue(instance, Convert.ChangeType(value, type));
        }
        return instance;
    }
}
