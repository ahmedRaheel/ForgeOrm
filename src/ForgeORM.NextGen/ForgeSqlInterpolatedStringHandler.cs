
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

[InterpolatedStringHandler]
public ref struct ForgeSqlInterpolatedStringHandler
{
    private StringBuilder _sql;
    private Dictionary<string, object?> _parameters;
    private int _index;

    /// <summary>
    /// Executes the ForgeSqlInterpolatedStringHandler operation.
    /// </summary>
    /// <param name="literalLength">The literalLength value.</param>
    /// <param name="formattedCount">The formattedCount value.</param>
    /// <returns>The result of the ForgeSqlInterpolatedStringHandler operation.</returns>
    public ForgeSqlInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _sql = new StringBuilder(literalLength + formattedCount * 8);
        _parameters = new Dictionary<string, object?>(formattedCount);
        _index = 0;
    }

    /// <summary>
    /// Executes the AppendLiteral operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    public void AppendLiteral(string value) => _sql.Append(value);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the T operation.</returns>
    public void AppendFormatted<T>(T value)
    {
        var name = "p" + _index++;
        _sql.Append('@').Append(name);
        _parameters[name] = value;
    }

    /// <summary>
    /// Executes the ToSql operation.
    /// </summary>
    /// <returns>The result of the ToSql operation.</returns>
    public ForgeSchemaAwareSql ToSql()
    {
        return new ForgeSchemaAwareSql
        {
            Sql = _sql.ToString(),
            Parameters = _parameters
        };
    }
}
