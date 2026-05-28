namespace ForgeORM.Providers.SqlServer;

internal static class SqlServerBulkFallbackPolicy
{
    public static bool CanFallback(Exception exception)
    {
        return exception is TypeLoadException
            or MissingMethodException
            or InvalidCastException
            or NotSupportedException
            or FileNotFoundException
            or FileLoadException;
    }
}
