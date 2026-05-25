
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public sealed class ForgeRequestReflector : IForgeRequestReflector
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="context">The context value.</param>
    /// <returns>The result of the T operation.</returns>
    public ForgeBuiltQuery ReflectRequest<T>(HttpContext context)
    {
        var table = typeof(T).Name;
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object?>();

        foreach (var query in context.Request.Query)
        {
            var key = query.Key;
            if (key.Equals("sort", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("page", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var param = "p_" + key;
            conditions.Add($"{key} = @{param}");
            parameters[param] = query.Value.ToString();
        }

        var sql = $"SELECT * FROM {table}";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);

        if (context.Request.Query.TryGetValue("sort", out var sort))
            sql += " ORDER BY " + sort;

        return new ForgeBuiltQuery { Sql = sql, Parameters = parameters };
    }
}
