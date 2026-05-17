using System.Reflection;

namespace ForgeORM.Core;

public sealed record ForgeSchemaDiff(string Entity, IReadOnlyList<string> Statements);

public static class ForgeSchemaMigrationExtensions
{
    public static ForgeSchemaDiff GenerateCreateTableScript<TEntity>(this ForgeDb db)
    {
        var type = typeof(TEntity);
        var shape = ForgeEntityShape.For(type);
        var columns = shape.ScalarProperties.Select(p => $"    {ForgeEntityShape.ColumnName(p)} {ToSqlType(p)}");
        var sql = $"CREATE TABLE {shape.TableName} (\n{string.Join(",\n", columns)}\n);";
        return new ForgeSchemaDiff(type.Name, [sql]);
    }

    public static Task<int> ApplyMigrationAsync(this ForgeDb db, ForgeSchemaDiff diff, CancellationToken cancellationToken = default)
    {
        return db.ExecuteAsync(string.Join("\n", diff.Statements), cancellationToken: cancellationToken);
    }

    private static string ToSqlType(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type == typeof(int)) return "INT";
        if (type == typeof(long)) return "BIGINT";
        if (type == typeof(decimal)) return "DECIMAL(18,2)";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "DATETIME2";
        if (type == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (type == typeof(bool)) return "BIT";
        return "NVARCHAR(MAX)";
    }
}
