using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

internal static class ForgeCompiledExecutionPlanCache
{
    private static readonly ConcurrentDictionary<ForgeCompiledExecutionPlanKey, object> Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static ForgeCompiledQueryPlan<T> GetOrAdd<T>(DbConnection connection, string sql, object? parameters, CommandType commandType, CommandBehavior behavior)
    {
        var provider = connection.GetType().FullName ?? connection.GetType().Name;
        var parameterType = parameters?.GetType();
        var sqlHash = ForgeFastHash.HashSql(sql);
        var key = new ForgeCompiledExecutionPlanKey(provider, typeof(T), parameterType, commandType, behavior, sqlHash);
        return (ForgeCompiledQueryPlan<T>)Cache.GetOrAdd(key, _ => new ForgeCompiledQueryPlan<T>
        {
            Sql = sql,
            CommandType = commandType,
            Behavior = behavior,
            Provider = provider,
            ParameterType = parameterType,
            QueryFingerprint = key.SqlFingerprint.ToString("X16", System.Globalization.CultureInfo.InvariantCulture),
            ParameterNames = ForgeParameterBinderCompiler.ExtractParameterNames(sql, commandType),
            Binder = ForgeParameterBinderCompiler.Compile(parameterType, sql, commandType, sqlHash),
            RequiresEnumNormalization = ForgeRawEnumSqlAnalyzer.RequiresNormalization<T>(sql, commandType)
        });
    }
}
