using ForgeORM.Caching.Redis;
using ForgeORM.Security;
using ForgeORM.Telemetry;

public static class CacheSecurityTelemetryEndpoints
{
    public static IEndpointRouteBuilder MapCacheSecurityTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/infrastructure").WithTags("09 Cache / Security / Telemetry");

        group.MapGet("/cache/demo", async (IForgeQueryCache cache) =>
        {
            var value = await cache.GetOrCreateAsync(
                "demo:products",
                _ => ValueTask.FromResult(new { CachedAtUtc = DateTimeOffset.UtcNow, Source = "ForgeORM cache" }),
                TimeSpan.FromMinutes(5));

            return Results.Ok(value);
        });

        group.MapPost("/security/validate-sql", (string sql, IForgeSqlSecurityValidator validator) =>
            Results.Ok(validator.Validate(sql)));

        group.MapGet("/security/mask-email", (string email, IForgeDataMasker masker) =>
            Results.Ok(new { original = email, masked = masker.MaskEmail(email) }));

        group.MapGet("/telemetry/snapshot", (IForgeTelemetry telemetry) =>
            Results.Ok(telemetry.Snapshot()));

        return app;
    }
}
