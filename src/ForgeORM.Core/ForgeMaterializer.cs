using System.Data.Common;

namespace ForgeORM.Core;

public static class ForgeMaterializer
{
    public static Func<DbDataReader, T> GetReader<T>(DbDataReader reader)
        => ForgeCompiledReaderResolver.GetReader<T>(reader);

    public static Func<DbDataReader, object> GetReader(Type type, DbDataReader reader)
        => ForgeCompiledReaderResolver.GetReader(type, reader);

    public static T Map<T>(DbDataReader reader)
        => ForgeCompiledReaderResolver.GetReader<T>(reader)(reader);

    public static object? Map(Type type, DbDataReader reader)
        => ForgeCompiledReaderResolver.GetReader(type, reader)(reader);

    public static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive
               || actual.IsEnum
               || actual == typeof(string)
               || actual == typeof(Guid)
               || actual == typeof(decimal)
               || actual == typeof(DateTime)
               || actual == typeof(DateTimeOffset)
               || actual == typeof(DateOnly)
               || actual == typeof(TimeOnly)
               || actual == typeof(TimeSpan)
               || actual == typeof(byte[]);
    }
}
