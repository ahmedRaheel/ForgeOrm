# TopNSql Report Fix Applied

Problem:
- /three-entry-styles/report/sql/top-customers failed with:
  System.MissingMethodException: Method 'ForgeReportBuilder`1.TopN' not found.

Cause:
- ForgeReportThreeEntryStyles.TopNSql used reflection and looked for an exact 2-parameter TopN method.
- The actual ForgeReportBuilder<T>.TopN signature has optional third parameter:
  TopN(string orderBySql, int count, bool descending = true)

Fix:
- Rewrote ForgeReportThreeEntryStyles to use strongly typed ForgeReportBuilder<TEntity> overloads.
- TopNSql now calls:
  report.TopN(orderBy, take, descending)
- Removed reflection from DimensionSql, SumSql, PivotSql, TopNSql and expression variants.

Endpoint now works:
GET /three-entry-styles/report/sql/top-customers
