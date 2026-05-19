# Advanced DB Surface + High Performance Frame Patch

Applied to ForgeOrm(21):

- `db.Search<T>().FullText(...).Fuzzy().Top(n).ToListAsync(ct)`
- `db.Vector<T>().SearchAsync(queryEmbedding, topK, VectorMetric.Cosine, ct)`
- `db.Graph().From<T>(id).Traverse(...).ShortestPathTo<TTo>(id).ToListAsync(ct)`
- `db.AI.OptimizeAsync(sql, ct)`
- `db.Workflow<TWorkflow>().StartAsync(request, ct)`
- `db.Jobs.EnqueueAsync(job, ct)`
- `db.Rules().EvaluateAsync<TResult>(ruleSet, facts, ct)`
- `db.Cube<T>().Dimension(...).Measure(...).BuildAsync(ct)`
- `db.Frame<T>().Where(expression).Parallel().MaxDegreeOfParallelism(n).SumAsync(...)`
- `frame.Vectorized().Where(...).Aggregate(...)`
- `frame.GroupBy(...).Sum(...).SortByDescending(...).Take(...)`
- `db.Set<T>().UseShard(...).UseShard(...).UnionShards().ToListAsync(ct)`
- `db.Set<T>().ReadIntoAsync(buffer, ct)` with ArrayPool-friendly overloads
- `app.MapForgeAiCteTempSamples()` restored
- `app.MapAdvancedDbSurfaceSamples()` added

These APIs are wired into the existing public methods/facades. No `Fast*` round-trip APIs were added.
