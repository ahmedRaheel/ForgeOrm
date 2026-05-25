using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

public sealed record DbDataReaderShape(string ProviderName, Type TargetType, DbDataReaderColumnShape[] Columns)
{
    public static DbDataReaderShape From(Type targetType, DbDataReader reader)
    {
        var columns = new DbDataReaderColumnShape[reader.FieldCount];
        System.Data.DataTable? schema = null;
        try { schema = reader.GetSchemaTable(); } catch { }

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var dbTypeName = string.Empty;
            var allowNull = true;
            try { dbTypeName = reader.GetDataTypeName(i) ?? string.Empty; } catch { }
            try { allowNull = schema is not null && i < schema.Rows.Count && schema.Rows[i]["AllowDBNull"] is bool b ? b : true; } catch { }
            columns[i] = new DbDataReaderColumnShape(reader.GetName(i), reader.GetFieldType(i), dbTypeName, allowNull);
        }
        return new DbDataReaderShape(reader.GetType().FullName ?? reader.GetType().Name, targetType, columns);
    }
}
