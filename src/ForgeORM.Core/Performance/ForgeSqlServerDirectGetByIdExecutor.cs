using System.Data;
using System.Threading;
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

    public static async Task<T?> ExecuteAsync(
        string connectionString,
        ForgeEntityMetadata metadata,
        object id,
        CancellationToken cancellationToken)
    {
        var plan = GetPlan(metadata);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        ForgeSqlServerProviderDirectHotPath.AddTypedParameter(command, plan.ParameterName, id, plan.KeyType);

        await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SingleRow | CommandBehavior.SingleResult | CommandBehavior.SequentialAccess,
                cancellationToken)
            .ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        return plan.Materialize(reader);
    }

    public static T? Execute(string connectionString, ForgeEntityMetadata metadata, object id)
    {
        var plan = GetPlan(metadata);

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        ForgeSqlServerProviderDirectHotPath.AddTypedParameter(command, plan.ParameterName, id, plan.KeyType);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
        if (!reader.Read())
            return default;

        return plan.Materialize(reader);
    }

    private static ExecutorPlan GetPlan(ForgeEntityMetadata metadata)
    {
        var plan = Volatile.Read(ref CachedPlan);
        var key = string.Concat(metadata.TableName, '|', metadata.KeyColumn, '|', metadata.EntityType.FullName);
        if (plan is not null && string.Equals(plan.MetadataKey, key, StringComparison.Ordinal))
            return plan;

        plan = ExecutorPlan.Create(metadata, key);
        Volatile.Write(ref CachedPlan, plan);
        return plan;
    }

    private sealed class ExecutorPlan
    {
        private Func<SqlDataReader, T>? _reader;

        private ExecutorPlan(string metadataKey, string sql, string parameterName, Type keyType)
        {
            MetadataKey = metadataKey;
            Sql = sql;
            ParameterName = parameterName;
            KeyType = keyType;
        }

        public string MetadataKey { get; }
        public string Sql { get; }
        public string ParameterName { get; }
        public Type KeyType { get; }

        public static ExecutorPlan Create(ForgeEntityMetadata metadata, string metadataKey)
        {
            var key = metadata.Properties.FirstOrDefault(x => x.IsKey || string.Equals(x.ColumnName, metadata.KeyColumn, StringComparison.OrdinalIgnoreCase));
            var keyType = key?.PropertyType ?? typeof(object);
            var parameterName = "@" + metadata.KeyColumn;
            var columns = metadata.Properties.Count == 0
                ? "*"
                : string.Join(", ", metadata.Properties.Where(x => !x.IsComputed).Select(x => x.ColumnName));
            var sql = $"SELECT TOP (1) {columns} FROM {metadata.TableName} WHERE {metadata.KeyColumn} = {parameterName}";
            return new ExecutorPlan(metadataKey, sql, parameterName, keyType);
        }

        public T Materialize(SqlDataReader reader)
        {
            var materializer = _reader;
            if (materializer is null)
            {
                materializer = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
                Volatile.Write(ref _reader, materializer);
            }

            return materializer(reader);
        }
    }
}
