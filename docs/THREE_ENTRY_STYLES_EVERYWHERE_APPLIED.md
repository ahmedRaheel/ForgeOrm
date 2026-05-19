# Three Entry Styles Everywhere Applied

Goal:
- Builder
- Raw SQL
- Expression

All three styles expose terminal materializers:
- ToListAsync
- ToDictionaryAsync
- ToJsonAsync
- ToDataFrameAsync
- ToCsvAsync
- ToSql

Added:
- src/ForgeORM.Core/EntryStyles/ForgeThreeEntryStyles.cs
- src/ForgeORM.Analytics/Reporting/ForgeReportThreeEntryStyles.cs
- samples/ForgeORM.Sample.Api/Endpoints/ThreeEntryStylesEndpoints.cs
- docs/api-design/THREE_ENTRY_STYLES.md
