using ForgeORM.Core;
using ForgeORM.QueryAst;
using ForgeORM.QueryAst.Artifacts;
using ForgeORM.SchemaOps;

public static class ArtifactEndpoints
{
    public static IEndpointRouteBuilder MapArtifactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/artifacts").WithTags("06 Query Artifacts / Views / Procedures");

        group.MapPost("/view/product-list", async (ForgeDbContext db, IForgeArtifactManager artifacts) =>
        {
            var query = ForgeSql.Select<Product>()
                .From("dbo.Products p")
                .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
                .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName");

            var artifact = query.AsView("vw_ProductList", "dbo")
                .WithReason("Create view from AST")
                .Render(db.Provider);

            return Results.Ok(await artifacts.CreateOrUpdateAsync(artifact.Artifact));
        });

        group.MapPost("/procedure/product-list", async (ForgeDbContext db, IForgeArtifactManager artifacts) =>
        {
            var query = ForgeSql.Select<Product>()
                .From("dbo.Products p")
                .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
                .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
                .WhereSql("p.Price >= @MinPrice");

            var artifact = query.AsProcedure("sp_ProductList_FromAst", "dbo")
                .WithParameter("@MinPrice", "DECIMAL(18,2)")
                .WithReason("Create stored procedure from AST")
                .Render(db.Provider);

            return Results.Ok(await artifacts.CreateOrUpdateAsync(artifact.Artifact));
        });

        return app;
    }
}
