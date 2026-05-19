# Sample API: Advanced Enterprise Feature Pack 1–20

The sample project now includes Swagger-visible endpoints under:

`/enterprise-advanced`

Endpoints:
1. `/enterprise-advanced/1-distributed-query-execution`
2. `/enterprise-advanced/2-distributed-cache`
3. `/enterprise-advanced/3-query-plan-analysis`
4. `/enterprise-advanced/4-automatic-query-optimization`
5. `/enterprise-advanced/5-adaptive-query-execution`
6. `/enterprise-advanced/6-async-streaming`
7. `/enterprise-advanced/7-columnar-analytics`
8. `/enterprise-advanced/8-materialized-query-cache`
9. `/enterprise-advanced/9-change-tracking-event-sourcing`
10. `/enterprise-advanced/10-database-observability`
11. `/enterprise-advanced/11-opentelemetry`
12. `/enterprise-advanced/12-source-generators`
13. `/enterprise-advanced/13-binary-protocol-optimizations`
14. `/enterprise-advanced/14-ai-native/to-sql`
15. `/enterprise-advanced/15-advanced-transactions`
16. `/enterprise-advanced/16-graphql-integration`
17. `/enterprise-advanced/17-data-virtualization`
18. `/enterprise-advanced/18-time-series-optimization`
19. `/enterprise-advanced/19-enterprise-migration-engine`
20. `/enterprise-advanced/20-enterprise-admin-portal`

Program.cs mapping added:

```csharp
app.MapEnterpriseAdvancedFeaturePackEndpoints();
```
