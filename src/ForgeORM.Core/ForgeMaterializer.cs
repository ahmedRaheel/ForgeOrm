using System.Data.Common;

namespace ForgeORM.Core;

internal static class ForgeMaterializer
{
    public static T Map<T>(DbDataReader reader)
        => ForgeIlMaterializerCache.GetOrCreate<T>(reader)(reader);

    public static object? Map(Type type, DbDataReader reader)
        => ForgeIlMaterializerCache.GetOrCreate(type, reader)(reader);

    internal static bool IsScalar(Type type)
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

    internal static object? GetDefault(Type type) => ForgeIlAccessors.DefaultValue(type);
}
