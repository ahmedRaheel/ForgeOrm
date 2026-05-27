# ForgeORM Performance Baseline

Known good direction after RuntimeEmit-only freeze:

## Query By Id

Target allocation:

```text
Dapper   ~7.82 KB
ForgeORM ~7.84 KB
```

Target latency:

```text
ForgeORM should remain near Dapper. Small variance is acceptable; large allocation jumps are not.
```

## Paged Search

Known good results were Dapper-level:

```text
Take 10:  ForgeORM around Dapper or faster
Take 50:  ForgeORM around Dapper
Take 100: ForgeORM around Dapper
```

## Freeze rule

If a change increases Query_By_Id allocation toward tens of KB, revert it immediately.

The known bad regression was around:

```text
ForgeORM_Query_By_Id ~48 KB allocation
```

That pattern indicates the hot path fell back to an expensive pipeline or duplicate planning/binding path.
