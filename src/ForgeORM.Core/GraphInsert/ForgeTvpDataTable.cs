using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static class ForgeTvpDataTable
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public static DataTable Create<T>(IReadOnlyList<T> rows)
    {
        var shape = ForgeEntityShape.For(typeof(T));
        var props = ForgeGraphWriteHelpers.GetInsertProperties(shape);
        var table = new DataTable();

        foreach (var prop in props)
            table.Columns.Add(ForgeEntityShape.ColumnName(prop), ForgeEnumConversion.StorageType(prop));

        foreach (var row in rows)
        {
            var values = props.Select(p => ForgeGraphWriteHelpers.NormalizeDatabaseValue(ForgeRuntimeAccessorCache.Get(p, row), p) ?? DBNull.Value).ToArray();
            table.Rows.Add(values);
        }

        return table;
    }
}
