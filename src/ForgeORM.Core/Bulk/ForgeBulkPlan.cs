using Microsoft.Data.SqlClient.Server;
using System.Data;
using System.Reflection;

namespace ForgeORM.Core;

public sealed class ForgeBulkPlan
{
    private readonly Func<object, object?>[] _getters;
    private readonly Type[] _declaredTypes;
    private readonly DataTable _schema;

    private ForgeBulkPlan(
        string quotedTableName,
        string tvpTypeName,
        string insertSql,
        PropertyInfo[] properties,
        string[] columnNames,
        Func<object, object?>[] getters,
        Type[] declaredTypes,
        DataTable schema,
        SqlMetaData[] sqlMetaData)
    {
        QuotedTableName = quotedTableName;
        TvpTypeName = tvpTypeName;
        InsertSql = insertSql;
        Properties = properties;
        ColumnNames = columnNames;
        _getters = getters;
        _declaredTypes = declaredTypes;
        _schema = schema;
        SqlMetaData = sqlMetaData;
    }

    public string QuotedTableName { get; }
    public string TvpTypeName { get; }
    public string InsertSql { get; }
    public SqlMetaData[] SqlMetaData { get; }
    public PropertyInfo[] Properties { get; }
    public string[] ColumnNames { get; }

    public static ForgeBulkPlan Create(Type entityType, string tableName)
    {
        var properties = GetBulkProperties(entityType, includeIdentity: false);
        var columnNames = BuildColumnNames(properties);
        return new ForgeBulkPlan(
            QuoteTable(tableName),
            BuildTvpTypeName(entityType),
            BuildInsertFromTvpSql(tableName, columnNames),
            properties,
            columnNames,
            BuildGetters(properties),
            BuildDeclaredTypes(properties),
            CreateSchema(properties),
            BuildSqlMetaData(properties));
    }

    public object? GetValue(object entity, int ordinal) => _getters[ordinal](entity);
    public Type GetDeclaredType(int ordinal) => _declaredTypes[ordinal];

    public DataTable CreateTable<T>(IReadOnlyList<T> rows)
    {
        var table = _schema.Clone();
        table.BeginLoadData();

        for (var r = 0; r < rows.Count; r++)
        {
            var dataRow = table.NewRow();
            var entity = rows[r]!;
            for (var c = 0; c < _getters.Length; c++)
                dataRow[c] = NormalizeValue(_getters[c](entity), _declaredTypes[c]) ?? DBNull.Value;
            table.Rows.Add(dataRow);
        }

        table.EndLoadData();
        return table;
    }

}

