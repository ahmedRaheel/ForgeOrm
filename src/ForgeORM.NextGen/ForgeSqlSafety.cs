using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public static class ForgeSqlSafety
{
    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="formattable">The formattable value.</param>
    /// <returns>The result of the From operation.</returns>
    public static ForgeSafeSql From(FormattableString formattable)
    {
        var sql = formattable.Format;
        var args = formattable.GetArguments();
        var parameters = new Dictionary<string, object?>();

        for (var i = 0; i < args.Length; i++)
        {
            var name = $"p{i}";
            sql = sql.Replace("{" + i + "}", "@" + name);
            parameters[name] = args[i];
        }

        return new ForgeSafeSql { Sql = sql, Parameters = parameters };
    }
}
