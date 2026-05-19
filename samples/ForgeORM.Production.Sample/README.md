# ForgeORM Production Sample

This sample folder documents the recommended production structure:

```text
Api/
  Endpoints/
Application/
  Orders/
  Customers/
Infrastructure/
  Persistence/
  Providers/
Domain/
  Entities/
```

Recommended features:

- Minimal API endpoints per feature, not everything in Program.cs.
- Query DTOs for optional search parameters.
- ForgeORM raw SQL and QueryAst examples.
- Stored procedure examples.
- Table-valued parameter examples.
- Temporal query examples.
- Bulk import from CSV/JSON examples.
- Vector search and AI query examples.
- Benchmark documentation and monitoring notes.
