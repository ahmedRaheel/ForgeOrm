using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ForgeDb>();
builder.Services.AddSingleton<ForgeArtifactManager>();
builder.Services.AddSingleton<ForgeDynamicQueryBuilder>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "ForgeORM Sample Scenarios API");

app.MapGet("/raw/products", async (ForgeDb db) =>
    await db.QueryAsync<Product>("SELECT * FROM dbo.Products"))
    .WithTags("01 Raw SQL");

app.MapGet("/raw/products/{id:int}", async (int id, ForgeDb db) =>
    await db.QuerySingleOrDefaultAsync<Product>("SELECT * FROM dbo.Products WHERE Id = @Id", new { Id = id }))
    .WithTags("01 Raw SQL");

app.MapGet("/stored-procedure/products", async (decimal minPrice, ForgeDb db) =>
    await db.QueryProcedureAsync<ProductListItem>("dbo.sp_GetProductList", new { MinPrice = minPrice }))
    .WithTags("02 Stored Procedures");

app.MapGet("/function/product-count", async (ForgeDb db) =>
    await db.ExecuteScalarAsync<int>("SELECT dbo.fn_ProductCount()"))
    .WithTags("03 Functions");

app.MapGet("/query-multiple/dashboard", async (ForgeDb db) =>
{
    using var grid = await db.QueryMultipleAsync("""
        SELECT COUNT(1) TotalProducts FROM dbo.Products;
        SELECT COUNT(1) TotalCustomers FROM dbo.Customers;
        SELECT TOP 5 * FROM dbo.Products ORDER BY Id DESC;
    """);

    return Results.Ok(new
    {
        ProductCount = await grid.ReadAsync<dynamic>(),
        CustomerCount = await grid.ReadAsync<dynamic>(),
        LatestProducts = await grid.ReadAsync<Product>()
    });
})
.WithTags("04 QueryMultiple");

app.MapGet("/builder/string/products", async (decimal minPrice, ForgeDynamicQueryBuilder qb, ForgeDb db) =>
{
    var q = qb.Select("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName", "b.Name AS BrandName")
        .From("dbo.Products p")
        .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .LeftJoin("dbo.Brands b", "b.Id = p.BrandId")
        .Where("p.Price > @MinPrice", new { MinPrice = minPrice })
        .OrderBy("p.Id DESC")
        .Take(20)
        .Build();

    return await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters);
})
.WithTags("05 String Builder");

app.MapGet("/builder/ast/products", async (decimal minPrice, ForgeDb db) =>
{
    var q = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .LeftJoin<Category>((p, c) => p.CategoryId == c.Id)
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
        .Where(x => x.Price > minPrice)
        .OrderByDescending(x => x.Id)
        .Take(20)
        .Render("SqlServer");

    return await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters);
})
.WithTags("06 AST Builder");

app.MapGet("/builder/ast/all-joins", async (ForgeDb db) =>
{
    var q = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .InnerJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .LeftJoin("dbo.Brands b", "b.Id = p.BrandId")
        .CrossApply("SELECT TOP 1 o.Id FROM dbo.Orders o ORDER BY o.Id DESC", "latestOrder")
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName", "b.Name AS BrandName")
        .OrderBySql("p.Id DESC")
        .Take(20)
        .Render("SqlServer");

    return await db.QueryAsync<ProductListItem>(q.Sql, q.Parameters);
})
.WithTags("06 AST Builder");

app.MapGet("/cte/latest-products", async (ForgeDb db) =>
{
    var q = ForgeSql.Select<Product>()
        .WithCte("LatestProducts", """
            SELECT *, ROW_NUMBER() OVER(PARTITION BY Code ORDER BY Id DESC) rn
            FROM dbo.Products
        """)
        .From("LatestProducts")
        .Columns("Id", "Code", "Name", "Price")
        .WhereSql("rn = 1")
        .Render("SqlServer");

    return await db.QueryAsync<Product>(q.Sql, q.Parameters);
})
.WithTags("07 CTE");

app.MapGet("/temp-table/script", () =>
{
    var script = ForgeSql.Script()
        .CreateTempTable("#ProductIds", t => t.Column("Id", "INT", false).PrimaryKey("Id"))
        .InsertIntoTemp("#ProductIds", "SELECT Id FROM dbo.Products WHERE Price > @MinPrice")
        .Statement("""
            SELECT p.*
            FROM dbo.Products p
            INNER JOIN #ProductIds ids ON ids.Id = p.Id
        """)
        .Render();

    return Results.Ok(script.Sql);
})
.WithTags("08 Temp Tables");

app.MapGet("/pagination/products", async (int page, int pageSize, ForgeDb db) =>
{
    var q = ForgeSql.Select<Product>()
        .From("dbo.Products")
        .Columns(x => x.Id, x => x.Code, x => x.Name, x => x.Price)
        .OrderByDescending(x => x.Id)
        .Skip(Math.Max(page - 1, 0) * pageSize)
        .Take(pageSize)
        .Render("SqlServer");

    return await db.QueryAsync<Product>(q.Sql, q.Parameters);
})
.WithTags("09 Pagination");

app.MapPost("/bulk/products", async (List<ProductCreateRequest> rows, ForgeDb db) =>
{
    await db.BulkInsertAsync("dbo.Products", rows);
    return Results.Ok(new { Inserted = rows.Count });
})
.WithTags("10 Bulk");

app.MapPost("/transaction/increase-prices", async (decimal amount, ForgeDb db) =>
{
    await using var connection = db.CreateConnection();
    await connection.OpenAsync();
    await using var tx = await connection.BeginTransactionAsync();

    try
    {
        await connection.ExecuteAsync("UPDATE dbo.Products SET Price = Price + @Amount", new { Amount = amount }, tx);
        await tx.CommitAsync();
        return Results.Ok(new { Updated = true });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
})
.WithTags("11 Transactions");

app.MapGet("/split/one-to-one", async (ForgeDb db) =>
{
    var rows = await db.SplitGraph<Customer>()
        .IncludeOne<CustomerProfile, int>(
            ids => "SELECT * FROM dbo.CustomerProfiles WHERE CustomerId IN @Ids",
            c => c.Id,
            p => p.CustomerId,
            (c, profile) => c.Profile = profile)
        .ToListAsync("SELECT * FROM dbo.Customers");

    return Results.Ok(rows);
})
.WithTags("12 Split Query");

app.MapGet("/split/one-to-many", async (ForgeDb db) =>
{
    var rows = await db.SplitGraph<Customer>()
        .IncludeMany<Order, int>(
            ids => "SELECT * FROM dbo.Orders WHERE CustomerId IN @Ids",
            c => c.Id,
            o => o.CustomerId,
            (c, orders) => c.Orders = orders.ToList())
        .ToListAsync("SELECT * FROM dbo.Customers");

    return Results.Ok(rows);
})
.WithTags("12 Split Query");

app.MapGet("/split/many-to-many", async (ForgeDb db) =>
{
    var rows = await db.SplitGraph<Product>()
        .IncludeManyToMany<ProductCategory, Category, int, int>(
            ids => "SELECT * FROM dbo.ProductCategories WHERE ProductId IN @Ids",
            ids => "SELECT * FROM dbo.Categories WHERE Id IN @Ids",
            p => p.Id,
            pc => pc.ProductId,
            pc => pc.CategoryId,
            c => c.Id,
            (p, cats) => p.Categories = cats.ToList())
        .ToListAsync("SELECT * FROM dbo.Products");

    return Results.Ok(rows);
})
.WithTags("12 Split Query");

app.MapPost("/artifacts/view/product-list", async (ForgeDb db, ForgeArtifactManager artifacts) =>
{
    var query = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName");

    var artifact = query.AsView("vw_ProductList", "dbo")
        .WithReason("Create view from AST")
        .Render("SqlServer");

    return Results.Ok(await artifacts.CreateOrUpdateAsync(artifact.Artifact));
})
.WithTags("13 Artifacts");

app.MapPost("/artifacts/procedure/product-list", async (ForgeDb db, ForgeArtifactManager artifacts) =>
{
    var query = ForgeSql.Select<Product>()
        .From("dbo.Products p")
        .LeftJoin("dbo.Categories c", "c.Id = p.CategoryId")
        .Columns("p.Id", "p.Code", "p.Name", "p.Price", "c.Name AS CategoryName")
        .WhereSql("p.Price >= @MinPrice");

    var artifact = query.AsProcedure("sp_ProductList_FromAst", "dbo")
        .WithParameter("@MinPrice", "DECIMAL(18,2)")
        .WithReason("Create stored procedure from AST")
        .Render("SqlServer");

    return Results.Ok(await artifacts.CreateOrUpdateAsync(artifact.Artifact));
})
.WithTags("13 Artifacts");


app.MapGet("/search/products/text", async (
    string? code,
    string? name,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDb db) =>
{
    return await db.Search<Product>()
        .FromSql("SELECT Id, Code, Name, Price FROM dbo.Products")
        .WhereIf(!string.IsNullOrWhiteSpace(code), "Code = @Code", new { Code = code })
        .WhereIf(!string.IsNullOrWhiteSpace(name), "Name LIKE @Name", new { Name = $"%{name}%" })
        .WhereIf(minPrice.HasValue, "Price >= @MinPrice", new { MinPrice = minPrice })
        .WhereIf(maxPrice.HasValue, "Price <= @MaxPrice", new { MaxPrice = maxPrice })
        .OrderBy("Id DESC")
        .Page(page, pageSize)
        .ToPagedAsync();
})
.WithTags("14 Universal Search");

app.MapGet("/search/products/expression", async (
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDb db) =>
{
    return await db.Search<Product>()
        .From("dbo.Products")
        .WhereIf(minPrice.HasValue, x => x.Price >= minPrice!.Value)
        .WhereIf(maxPrice.HasValue, x => x.Price <= maxPrice!.Value)
        .OrderBy("Id DESC")
        .Page(page, pageSize)
        .ToPagedAsync();
})
.WithTags("14 Universal Search");

app.MapGet("/search/products/builder", async (
    string? code,
    string? name,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDb db) =>
{
    return await db.Search<Product>()
        .Select("Id", "Code", "Name", "Price")
        .From("dbo.Products")
        .Optional("Code", code)
        .OptionalLike("Name", name)
        .OptionalBetween("Price", minPrice, maxPrice)
        .OrderBy("Id DESC")
        .Page(page, pageSize)
        .ToPagedAsync();
})
.WithTags("14 Universal Search");

app.MapGet("/search/products/procedure", async (
    string? code,
    string? name,
    decimal? minPrice,
    decimal? maxPrice,
    int page,
    int pageSize,
    ForgeDb db) =>
{
    return await db.SearchProcedure<Product>("dbo.SearchProducts")
        .WithOptional("Code", code)
        .WithOptional("Name", name)
        .WithOptional("MinPrice", minPrice)
        .WithOptional("MaxPrice", maxPrice)
        .Page(page, pageSize)
        .ToListAsync();
})
.WithTags("14 Universal Search");

app.Run();

public sealed class ForgeDb
{
    private readonly string _connectionString;
    public ForgeDb(IConfiguration config) => _connectionString = config.GetConnectionString("DefaultConnection")!;
    public DbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        await using var c = CreateConnection();
        return (await c.QueryAsync<T>(sql, parameters)).ToList();
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        await using var c = CreateConnection();
        return await c.QuerySingleOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        await using var c = CreateConnection();
        return await c.ExecuteScalarAsync<T>(sql, parameters);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        await using var c = CreateConnection();
        return await c.ExecuteAsync(sql, parameters);
    }

    public async Task<GridWrapper> QueryMultipleAsync(string sql, object? parameters = null)
    {
        var c = CreateConnection();
        await c.OpenAsync();
        var grid = await c.QueryMultipleAsync(sql, parameters);
        return new GridWrapper(c, grid);
    }

    public async Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string name, object? parameters = null)
    {
        await using var c = CreateConnection();
        return (await c.QueryAsync<T>(name, parameters, commandType: CommandType.StoredProcedure)).ToList();
    }

    public async Task BulkInsertAsync<T>(string tableName, IReadOnlyCollection<T> rows)
    {
        if (rows.Count == 0) return;
        var props = typeof(T).GetProperties().Where(x => x.Name != "Id").ToList();
        var cols = string.Join(", ", props.Select(x => x.Name));
        var vals = string.Join(", ", props.Select(x => "@" + x.Name));
        await ExecuteAsync($"INSERT INTO {tableName} ({cols}) VALUES ({vals})", rows);
    }
}

public sealed class GridWrapper : IDisposable
{
    private readonly DbConnection _connection;
    private readonly SqlMapper.GridReader _reader;
    public GridWrapper(DbConnection connection, SqlMapper.GridReader reader) { _connection = connection; _reader = reader; }
    public async Task<IReadOnlyList<T>> ReadAsync<T>() => (await _reader.ReadAsync<T>()).ToList();
    public void Dispose() { _reader.Dispose(); _connection.Dispose(); }
}

public static class ForgeSql
{
    public static ForgeAstSelectBuilder<T> Select<T>() => new();
    public static ForgeScriptBuilder Script() => new();
}

public sealed record ForgeRenderedSql(string Sql, object? Parameters = null);
public sealed record ForgeCte(string Name, string Sql);

public sealed class ForgeAstSelectBuilder<T>
{
    private readonly List<string> _columns = [];
    private readonly List<string> _joins = [];
    private readonly List<string> _where = [];
    private readonly List<string> _groupBy = [];
    private readonly List<ForgeCte> _ctes = [];
    private readonly Dictionary<string, object?> _parameters = [];
    private int _parameterIndex;
    private string? _table;
    private string? _orderBy;
    private int? _skip;
    private int? _take;

    public ForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns) { _columns.AddRange(columns.Select(MemberName)); return this; }
    public ForgeAstSelectBuilder<T> Columns(params string[] columns) { _columns.AddRange(columns); return this; }
    public ForgeAstSelectBuilder<T> From(string? table = null) { _table = table ?? typeof(T).Name; return this; }
    public ForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        if (predicate.Body is not BinaryExpression b) throw new NotSupportedException("Only simple where is supported.");
        var p = "p" + _parameterIndex++;
        _parameters[p] = Expression.Lambda(b.Right).Compile().DynamicInvoke();
        _where.Add($"{Member(b.Left)} {Op(b.NodeType)} @{p}");
        return this;
    }
    public ForgeAstSelectBuilder<T> WhereSql(string condition) { _where.Add(condition); return this; }
    public ForgeAstSelectBuilder<T> Join(string table, string on) => InnerJoin(table, on);
    public ForgeAstSelectBuilder<T> InnerJoin(string table, string on) { _joins.Add($"INNER JOIN {table} ON {on}"); return this; }
    public ForgeAstSelectBuilder<T> LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    public ForgeAstSelectBuilder<T> CrossApply(string expression, string alias) { _joins.Add($"CROSS APPLY ({expression}) {alias}"); return this; }
    public ForgeAstSelectBuilder<T> LeftJoin<TJoin>(Expression<Func<T, TJoin, bool>> on) { _joins.Add($"LEFT JOIN {typeof(TJoin).Name}s ON {JoinCondition(on)}"); return this; }
    public ForgeAstSelectBuilder<T> WithCte(string name, string sql) { _ctes.Add(new ForgeCte(name, sql)); return this; }
    public ForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column) { _orderBy = $"{MemberName(column)} DESC"; return this; }
    public ForgeAstSelectBuilder<T> OrderBySql(string orderBy) { _orderBy = orderBy; return this; }
    public ForgeAstSelectBuilder<T> Skip(int rows) { _skip = rows; return this; }
    public ForgeAstSelectBuilder<T> Take(int rows) { _take = rows; return this; }

    public ForgeRenderedSql Render(string provider)
    {
        var sb = new StringBuilder();
        if (_ctes.Count > 0) sb.Append("WITH ").Append(string.Join(", ", _ctes.Select(x => $"{x.Name} AS ({x.Sql})"))).AppendLine();
        sb.Append("SELECT ").Append(_columns.Count == 0 ? "*" : string.Join(", ", _columns)).Append(" FROM ").Append(_table ?? typeof(T).Name);
        if (_joins.Count > 0) sb.Append(' ').Append(string.Join(" ", _joins));
        if (_where.Count > 0) sb.Append(" WHERE ").Append(string.Join(" AND ", _where));
        if (!string.IsNullOrWhiteSpace(_orderBy)) sb.Append(" ORDER BY ").Append(_orderBy);
        if (_take.HasValue) sb.Append($" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take.Value} ROWS ONLY");
        return new ForgeRenderedSql(sb.ToString(), _parameters);
    }

    public ForgeViewArtifactBuilder<T> AsView(string name, string schema = "dbo") => new(this, name, schema);
    public ForgeProcedureArtifactBuilder<T> AsProcedure(string name, string schema = "dbo") => new(this, name, schema);

    private static string MemberName(Expression<Func<T, object>> e) => e.Body is UnaryExpression u && u.Operand is MemberExpression m ? m.Member.Name : ((MemberExpression)e.Body).Member.Name;
    private static string Member(Expression e) => ((MemberExpression)e).Member.Name;
    private static string Op(ExpressionType t) => t switch { ExpressionType.GreaterThan => ">", ExpressionType.Equal => "=", ExpressionType.LessThan => "<", _ => "=" };
    private static string JoinCondition<TJoin>(Expression<Func<T, TJoin, bool>> e)
    {
        var b = (BinaryExpression)e.Body;
        return $"{JoinMember(b.Left)} {Op(b.NodeType)} {JoinMember(b.Right)}";
    }
    private static string JoinMember(Expression e)
    {
        var m = (MemberExpression)e;
        var p = (ParameterExpression)m.Expression!;
        return $"{p.Name}.{m.Member.Name}";
    }
}

public sealed class ForgeDynamicQueryBuilder
{
    public ForgeDynamicSelectBuilder Select(params string[] columns) => new(columns);
}

public sealed class ForgeDynamicSelectBuilder
{
    private readonly List<string> _columns;
    private readonly List<string> _joins = [];
    private string? _table;
    private string? _where;
    private string? _orderBy;
    private int? _take;
    private object? _parameters;
    public ForgeDynamicSelectBuilder(IEnumerable<string> columns) => _columns = columns.ToList();
    public ForgeDynamicSelectBuilder From(string table) { _table = table; return this; }
    public ForgeDynamicSelectBuilder LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    public ForgeDynamicSelectBuilder Where(string where, object? parameters = null) { _where = where; _parameters = parameters; return this; }
    public ForgeDynamicSelectBuilder OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    public ForgeDynamicSelectBuilder Take(int take) { _take = take; return this; }
    public ForgeRenderedSql Build()
    {
        var sql = $"SELECT {string.Join(", ", _columns)} FROM {_table} {string.Join(" ", _joins)}";
        if (_where != null) sql += " WHERE " + _where;
        if (_orderBy != null) sql += " ORDER BY " + _orderBy;
        if (_take.HasValue) sql += $" OFFSET 0 ROWS FETCH NEXT {_take.Value} ROWS ONLY";
        return new ForgeRenderedSql(sql, _parameters);
    }
}

public sealed class ForgeScriptBuilder
{
    private readonly List<string> _items = [];
    public ForgeScriptBuilder CreateTempTable(string name, Action<TempBuilder> configure) { var t = new TempBuilder(name); configure(t); _items.Add(t.Sql()); return this; }
    public ForgeScriptBuilder InsertIntoTemp(string table, string select) { _items.Add($"INSERT INTO {table} {select}"); return this; }
    public ForgeScriptBuilder Statement(string sql) { _items.Add(sql); return this; }
    public ForgeRenderedSql Render() => new(string.Join(Environment.NewLine, _items.Select(x => x.TrimEnd(';') + ";")));
}
public sealed class TempBuilder
{
    private readonly string _name; private readonly List<string> _cols = [];
    public TempBuilder(string name) => _name = name;
    public TempBuilder Column(string name, string type, bool nullable = true) { _cols.Add($"{name} {type} {(nullable ? "NULL" : "NOT NULL")}"); return this; }
    public TempBuilder PrimaryKey(params string[] cols) { _cols.Add($"PRIMARY KEY ({string.Join(", ", cols)})"); return this; }
    public string Sql() => $"CREATE TABLE {_name} ({string.Join(", ", _cols)})";
}

public sealed class ForgeViewArtifactBuilder<T>
{
    private readonly ForgeAstSelectBuilder<T> _q; private readonly string _name; private readonly string _schema; private string? _reason;
    public ForgeViewArtifactBuilder(ForgeAstSelectBuilder<T> q, string name, string schema) { _q = q; _name = name; _schema = schema; }
    public ForgeViewArtifactBuilder<T> WithReason(string reason) { _reason = reason; return this; }
    public ForgeArtifactRenderResult Render(string provider) { var q = _q.Render(provider); var sql = $"CREATE OR ALTER VIEW {_schema}.{_name} AS {q.Sql}"; return new(new(ForgeDbArtifactType.View, _schema, _name, sql, _reason), sql); }
}
public sealed class ForgeProcedureArtifactBuilder<T>
{
    private readonly ForgeAstSelectBuilder<T> _q; private readonly string _name; private readonly string _schema; private readonly List<string> _params = []; private string? _reason;
    public ForgeProcedureArtifactBuilder(ForgeAstSelectBuilder<T> q, string name, string schema) { _q = q; _name = name; _schema = schema; }
    public ForgeProcedureArtifactBuilder<T> WithParameter(string name, string type) { _params.Add($"{name} {type}"); return this; }
    public ForgeProcedureArtifactBuilder<T> WithReason(string reason) { _reason = reason; return this; }
    public ForgeArtifactRenderResult Render(string provider) { var q = _q.Render(provider); var sql = $"CREATE OR ALTER PROCEDURE {_schema}.{_name} {string.Join(",", _params)} AS BEGIN SET NOCOUNT ON; {q.Sql} END"; return new(new(ForgeDbArtifactType.StoredProcedure, _schema, _name, sql, _reason), sql); }
}
public enum ForgeDbArtifactType { View, StoredProcedure }
public sealed record ForgeDbArtifact(ForgeDbArtifactType Type, string Schema, string Name, string SqlDefinition, string? ChangeReason);
public sealed record ForgeArtifactRenderResult(ForgeDbArtifact Artifact, string DeploymentSql);

public sealed class ForgeArtifactManager
{
    private readonly ForgeDb _db;
    public ForgeArtifactManager(ForgeDb db) => _db = db;
    public async Task<object> CreateOrUpdateAsync(ForgeDbArtifact a)
    {
        await _db.ExecuteAsync("""
            IF OBJECT_ID('dbo.ForgeOrmArtifactHistory', 'U') IS NULL
            CREATE TABLE dbo.ForgeOrmArtifactHistory
            (
                Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                ArtifactType NVARCHAR(50), SchemaName NVARCHAR(128), ArtifactName NVARCHAR(256),
                VersionNo INT, SqlHash NVARCHAR(128), SqlDefinition NVARCHAR(MAX),
                AppliedAtUtc DATETIME2 DEFAULT SYSUTCDATETIME()
            );
        """);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(a.SqlDefinition)));
        var latest = await _db.QuerySingleOrDefaultAsync<dynamic>("SELECT TOP 1 * FROM dbo.ForgeOrmArtifactHistory WHERE ArtifactType=@t AND SchemaName=@s AND ArtifactName=@n ORDER BY VersionNo DESC", new { t = a.Type.ToString(), s = a.Schema, n = a.Name });
        if (latest != null && latest.SqlHash == hash) return new { a.Name, Applied = false, SkippedBecauseUnchanged = true };
        await _db.ExecuteAsync(a.SqlDefinition);
        await _db.ExecuteAsync("INSERT INTO dbo.ForgeOrmArtifactHistory(ArtifactType,SchemaName,ArtifactName,VersionNo,SqlHash,SqlDefinition) VALUES(@t,@s,@n,@v,@h,@sql)",
            new { t = a.Type.ToString(), s = a.Schema, n = a.Name, v = latest == null ? 1 : ((int)latest.VersionNo + 1), h = hash, sql = a.SqlDefinition });
        return new { a.Name, Applied = true };
    }
}

public static class SplitGraphExtensions
{
    public static SplitGraph<T> SplitGraph<T>(this ForgeDb db) => new(db);
}
public sealed class SplitGraph<T>
{
    private readonly ForgeDb _db; private readonly List<Func<IReadOnlyList<T>, Task>> _loaders = [];
    public SplitGraph(ForgeDb db) => _db = db;
    public SplitGraph<T> IncludeOne<TChild, TKey>(Func<IReadOnlyCollection<TKey>, string> sql, Func<T, TKey> pk, Func<TChild, TKey> fk, Action<T, TChild?> assign) where TKey : notnull
    {
        _loaders.Add(async parents => { var keys = parents.Select(pk).Distinct().ToList(); var children = await _db.QueryAsync<TChild>(sql(keys), new { Ids = keys }); var lookup = children.GroupBy(fk).ToDictionary(x => x.Key, x => x.FirstOrDefault()); foreach (var p in parents) assign(p, lookup.TryGetValue(pk(p), out var c) ? c : default); }); return this;
    }
    public SplitGraph<T> IncludeMany<TChild, TKey>(Func<IReadOnlyCollection<TKey>, string> sql, Func<T, TKey> pk, Func<TChild, TKey> fk, Action<T, IReadOnlyList<TChild>> assign) where TKey : notnull
    {
        _loaders.Add(async parents => { var keys = parents.Select(pk).Distinct().ToList(); var children = await _db.QueryAsync<TChild>(sql(keys), new { Ids = keys }); var lookup = children.GroupBy(fk).ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList()); foreach (var p in parents) assign(p, lookup.TryGetValue(pk(p), out var rows) ? rows : []); }); return this;
    }
    public SplitGraph<T> IncludeManyToMany<TJoin, TChild, TPk, TCk>(Func<IReadOnlyCollection<TPk>, string> joinSql, Func<IReadOnlyCollection<TCk>, string> childSql, Func<T, TPk> pk, Func<TJoin, TPk> jpk, Func<TJoin, TCk> jck, Func<TChild, TCk> ck, Action<T, IReadOnlyList<TChild>> assign) where TPk : notnull where TCk : notnull
    {
        _loaders.Add(async parents => { var pks = parents.Select(pk).Distinct().ToList(); var joins = await _db.QueryAsync<TJoin>(joinSql(pks), new { Ids = pks }); var cks = joins.Select(jck).Distinct().ToList(); var children = await _db.QueryAsync<TChild>(childSql(cks), new { Ids = cks }); var childLookup = children.ToDictionary(ck); var joinLookup = joins.GroupBy(jpk).ToDictionary(x => x.Key, x => x.Select(jck).ToList()); foreach (var p in parents) assign(p, joinLookup.TryGetValue(pk(p), out var ids) ? ids.Where(childLookup.ContainsKey).Select(x => childLookup[x]).ToList() : []); }); return this;
    }
    public async Task<IReadOnlyList<T>> ToListAsync(string sql) { var parents = await _db.QueryAsync<T>(sql); foreach (var loader in _loaders) await loader(parents); return parents; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ForgeTableAttribute : Attribute { public string Name { get; } public ForgeTableAttribute(string name) => Name = name; }

[ForgeTable("Products")]
public sealed class Product { public int Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Price { get; set; } public int? CategoryId { get; set; } public int? BrandId { get; set; } public List<Category> Categories { get; set; } = []; }
[ForgeTable("Categories")]
public sealed class Category { public int Id { get; set; } public string Name { get; set; } = ""; }
[ForgeTable("Brands")]
public sealed class Brand { public int Id { get; set; } public string Name { get; set; } = ""; }
[ForgeTable("Customers")]
public sealed class Customer { public int Id { get; set; } public string Name { get; set; } = ""; public string Email { get; set; } = ""; public CustomerProfile? Profile { get; set; } public List<Order> Orders { get; set; } = []; }
public sealed class CustomerProfile { public int Id { get; set; } public int CustomerId { get; set; } public string Phone { get; set; } = ""; public string City { get; set; } = ""; }
public sealed class Order { public int Id { get; set; } public int CustomerId { get; set; } public DateTime OrderDate { get; set; } public decimal TotalAmount { get; set; } }
public sealed class ProductCategory { public int ProductId { get; set; } public int CategoryId { get; set; } }
public sealed class ProductListItem { public int Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Price { get; set; } public string? CategoryName { get; set; } public string? BrandName { get; set; } }
public sealed class ProductCreateRequest { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Price { get; set; } public int? CategoryId { get; set; } public int? BrandId { get; set; } }
