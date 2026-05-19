# User-Friendly Terminal Materializers Applied

Goal:
- Maximum feature power with minimum user actions.
- Terminal methods execute and materialize automatically.

Added:
- QueryDictionaryAsync
- QueryJsonProjectionAsync
- QueryJsonAsync
- QueryDataFrameAsync
- QueryCsvAsync
- Report ToDictionaryAsync
- Report ToJsonAsync
- Report ToDataFrameAsync
- Report ToCsvAsync
- Report ToDtoListAsync
- Report ToSqlProjection
- Expression overload helpers for Dimension, Sum, Avg, Min, Max, Count, Pivot, TopN
- User-friendly sample endpoints under /user-friendly-reporting

Important:
- Dynamic reports/pivots use ToDictionaryAsync, ToJsonAsync or ToDataFrameAsync.
- Fixed-shape reports can use ToDtoListAsync<TDto>.
- ToSqlProjection is for preview/debug only.
