using System.Data;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Application-start warmup helpers for ForgeORM direct and generated query paths.
/// Use this in benchmarks and high-throughput services to prime direct plan caches before the hot path is measured.
/// </summary>
public static class ForgeQueryWarmup
{
    /// <summary>
    /// Precompiles the framework-level direct plan for a SQL command shape.
    /// </summary>
    public static bool Precompile(string sql, object? parameters = null, CommandType commandType = CommandType.Text)
        => ForgeDirectQueryExecutor.Precompile(sql, parameters, commandType);

    /// <summary>
    /// Precompiles multiple SQL command shapes.
    /// </summary>
    public static int PrecompileAll(params (string Sql, object? Parameters)[] commands)
    {
        if (commands is null || commands.Length == 0)
            return 0;

        var count = 0;
        for (var i = 0; i < commands.Length; i++)
        {
            if (ForgeDirectQueryExecutor.Precompile(commands[i].Sql, commands[i].Parameters, CommandType.Text))
                count++;
        }

        return count;
    }
}
