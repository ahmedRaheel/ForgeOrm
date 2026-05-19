# ForgeORM Enterprise Feature Pack 1-10

This package adds compile-safe foundations and sample endpoints for the requested 10 areas:

1. Complex query power: EXISTS, NOT EXISTS, subquery IN, CASE WHEN, JSON_VALUE, full-text, temporal table helpers, lateral/CROSS APPLY, recursive CTE foundation.
2. Query performance engine: Profile, Analyze, Explain, Validate, Debug SQL, Tags, Comments, Timeout, CacheFor, compiled query wrapper, StreamAsync.
3. Advanced mapping: projection/debug SQL foundations for multi-map/nested DTO scenarios.
4. DataFrame enterprise: Join, FillNull, DropDuplicates, NormalizeColumn, MovingAverage, DetectOutliers, Correlation, ExportCsvText, ExportHtmlTable.
5. Migration/schema: GenerateCreateTableScript and ApplyMigrationAsync foundation.
6. Multi-tenant support: ForTenant query filter helper and tenant context record.
7. Security: SQL safety validator and masking helpers.
8. Bulk sync: SyncAsync foundation with options for insert/update/delete-missing/provider strategy.
9. Event/outbox: ForgeOutboxMessage, SaveOutboxAsync and SaveWithOutboxAsync foundations.
10. Developer experience: ToSql, ToDebugSql, Explain, Validate, Clone, Tag, Comment covered in query builder.

Sample endpoint group added:

```csharp
app.MapEnterpriseFeatureEndpoints();
```

Endpoint route prefix:

```text
/enterprise-features
```

Note: Provider-specific internal implementations for TVP/MERGE/COPY/ON CONFLICT/Oracle MERGE can now plug into the public API surface without changing the samples.
