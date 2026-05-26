using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Per-entity SQL Server GetById executor. This is the true hot path used by ForgeDb.GetByIdAsync.
/// It avoids DbConnection/DbCommand/DbDataReader abstractions, avoids the generic query pipeline,
/// avoids List allocation, avoids per-call SQL generation, and caches the materializer after the
/// first result shape is seen.
/// </summary>
internal static class ForgeSqlServerDirectGetByIdExecutor<T>
{
    private static ExecutorPlan? CachedPlan;

    public static async ValueTask<T?> ExecuteAsync(
        string connectionString,
        ForgeEntityMetadata metadata,
        object id,
        CancellationToken cancellationToken)
    {
        var plan = GetOrCreatePlan(metadata);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        ForgeSqlServerProviderDirectHotPath.AddTypedParameter(command, plan.ParameterName, id, plan.KeyType);

        await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SingleRow | CommandBehavior.SequentialAccess,
                cancellationToken)
            .ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        return plan.Materialize(reader);
    }

    public static T? Execute(string connectionString, ForgeEntityMetadata metadata, object id)
    {
        var plan = GetOrCreatePlan(metadata);

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        ForgeSqlServerProviderDirectHotPath.AddTypedParameter(command, plan.ParameterName, id, plan.KeyType);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        if (!reader.Read())
            return default;

        return plan.Materialize(reader);
    }

    private static ExecutorPlan GetOrCreatePlan(ForgeEntityMetadata metadata)
    {
        // One static executor plan per closed T. Metadata is resolved before the hot path.
        // Avoid rebuilding SQL or comparing strings on every GetById call.
        var plan = Volatile.Read(ref CachedPlan);
        if (plan is not null)
            return plan;

        plan = ExecutorPlan.Create(metadata);
        Volatile.Write(ref CachedPlan, plan);
        return plan;
    }

    private sealed class ExecutorPlan
    {
        private Func<SqlDataReader, T>? _runtimeEmitReader;
        private Func<SqlDataReader, T>? _sourceGeneratedReader;

        private ExecutorPlan(string sql, string parameterName, Type keyType)
        {
            Sql = sql;
            ParameterName = parameterName;
            KeyType = keyType;
        }

        public string Sql { get; }
        public string ParameterName { get; }
        public Type KeyType { get; }

        public static ExecutorPlan Create(ForgeEntityMetadata metadata)
        {
            ForgePropertyMetadata? key = null;
            for (var i = 0; i < metadata.Properties.Count; i++)
            {
                var property = metadata.Properties[i];
                if (property.IsKey || string.Equals(property.ColumnName, metadata.KeyColumn, StringComparison.OrdinalIgnoreCase))
                {
                    key = property;
                    break;
                }
            }

            var keyType = key?.PropertyType ?? typeof(object);
            var parameterName = "@" + metadata.KeyColumn;
            var sql = BuildSql(metadata);
            return new ExecutorPlan(sql, parameterName, keyType);
        }

        private static string BuildSql(ForgeEntityMetadata metadata)
        {
            var builder = new StringBuilder(metadata.Properties.Count * 24);
            for (var i = 0; i < metadata.Properties.Count; i++)
            {
                var columnName = metadata.Properties[i].ColumnName;
                if (string.IsNullOrWhiteSpace(columnName))
                    continue;

                if (builder.Length != 0)
                    builder.Append(", ");

                builder.Append(columnName);
            }

            var columns = builder.Length == 0 ? "*" : builder.ToString();
            return $"SELECT TOP (1) {columns} FROM {metadata.TableName} WHERE {metadata.KeyColumn} = @{metadata.KeyColumn}";
        }

        public T Materialize(SqlDataReader reader)
        {
            var runtime = _runtimeEmitReader;
            if (runtime is null)
            {
                runtime = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
                Volatile.Write(ref _runtimeEmitReader, runtime);
            }

            return runtime(reader);
        }
    }
}
