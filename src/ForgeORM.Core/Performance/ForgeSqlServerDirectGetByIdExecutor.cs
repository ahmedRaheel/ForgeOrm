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

    public static ValueTask<T?> ExecuteAsync(
        string connectionString,
        ForgeEntityMetadata metadata,
        object id,
        CancellationToken cancellationToken)
        => ExecuteAsync<object?>(connectionString, metadata, id, cancellationToken);

    public static async ValueTask<T?> ExecuteAsync<TKey>(
        string connectionString,
        ForgeEntityMetadata metadata,
        TKey id,
        CancellationToken cancellationToken)
    {
        var plan = GetOrCreatePlan<TKey>(metadata);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        plan.BindParameter(command, id);

        await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SingleRow | CommandBehavior.SequentialAccess,
                cancellationToken)
            .ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        return plan.Materialize(reader);
    }

    public static T? Execute(string connectionString, ForgeEntityMetadata metadata, object id)
        => Execute<object?>(connectionString, metadata, id);

    public static T? Execute<TKey>(string connectionString, ForgeEntityMetadata metadata, TKey id)
    {
        var plan = GetOrCreatePlan<TKey>(metadata);

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        plan.BindParameter(command, id);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        if (!reader.Read())
            return default;

        return plan.Materialize(reader);
    }

    private static ExecutorPlan GetOrCreatePlan(ForgeEntityMetadata metadata)
    {
        // Compatibility cache for object-based callers. Prefer Execute<TKey> from public APIs.
        var plan = Volatile.Read(ref CachedPlan);
        if (plan is not null)
            return plan;

        plan = ExecutorPlan.Create(metadata);
        Volatile.Write(ref CachedPlan, plan);
        return plan;
    }

    private static ExecutorPlan<TKey> GetOrCreatePlan<TKey>(ForgeEntityMetadata metadata)
    {
        var plan = TypedPlanCache<TKey>.CachedPlan;
        if (plan is not null)
            return plan;

        plan = ExecutorPlan<TKey>.Create(metadata);
        Volatile.Write(ref TypedPlanCache<TKey>.CachedPlan, plan);
        return plan;
    }

    private static class TypedPlanCache<TKey>
    {
        public static ExecutorPlan<TKey>? CachedPlan;
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

        internal static string BuildSql(ForgeEntityMetadata metadata)
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
            // Keep separate cached materializers per compilation mode. Otherwise a benchmark that
            // switches RuntimeEmit <-> SourceGenerated in the same process reuses the first delegate
            // and both modes appear to have identical allocation/ratio numbers.
            if (ForgeSourceGeneratedRegistry.CompilationMode == ForgeOrmCompilationMode.RuntimeEmit)
            {
                var runtime = _runtimeEmitReader;
                if (runtime is null)
                {
                    runtime = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
                    Volatile.Write(ref _runtimeEmitReader, runtime);
                }

                return runtime(reader);
            }

            var generated = _sourceGeneratedReader;
            if (generated is null)
            {
                generated = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
                Volatile.Write(ref _sourceGeneratedReader, generated);
            }

            return generated(reader);
        }
    }

    private sealed class ExecutorPlan<TKey>
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

        public static ExecutorPlan<TKey> Create(ForgeEntityMetadata metadata)
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

            var keyType = key?.PropertyType ?? typeof(TKey);
            var parameterName = "@" + metadata.KeyColumn;
            var sql = ExecutorPlan.BuildSql(metadata);
            return new ExecutorPlan<TKey>(sql, parameterName, keyType);
        }

        public void BindParameter(SqlCommand command, TKey id)
            => ForgeSqlServerProviderDirectHotPath.AddTypedParameter(command, ParameterName, id, typeof(TKey));

        public T Materialize(SqlDataReader reader)
        {
            if (ForgeSourceGeneratedRegistry.CompilationMode == ForgeOrmCompilationMode.RuntimeEmit)
            {
                var runtime = _runtimeEmitReader;
                if (runtime is null)
                {
                    runtime = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
                    Volatile.Write(ref _runtimeEmitReader, runtime);
                }

                return runtime(reader);
            }

            var generated = _sourceGeneratedReader;
            if (generated is null)
            {
                generated = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
                Volatile.Write(ref _sourceGeneratedReader, generated);
            }

            return generated(reader);
        }
    }

}
