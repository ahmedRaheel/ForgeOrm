namespace ForgeORM.Core.Performance;

internal static class ForgeScalarConverter
{
    public static T? To<T>(object? value)
    {
        if (value is null || value is DBNull)
            return default;

        var targetType = typeof(T);
        var nullable = Nullable.GetUnderlyingType(targetType);
        var actualType = nullable ?? targetType;

        if (actualType.IsEnum)
        {
            if (value is string text)
                return (T)Enum.Parse(actualType, text, ignoreCase: true);
            return (T)Enum.ToObject(actualType, value);
        }

        if (actualType == typeof(Guid) && value is string guidText)
            return (T)(object)Guid.Parse(guidText);

        if (actualType == typeof(DateOnly) && value is DateTime dt)
            return (T)(object)DateOnly.FromDateTime(dt);

        if (actualType == typeof(TimeOnly) && value is TimeSpan ts)
            return (T)(object)TimeOnly.FromTimeSpan(ts);

        if (value is T typed)
            return typed;

        return (T)Convert.ChangeType(value, actualType, System.Globalization.CultureInfo.InvariantCulture);
    }
}
