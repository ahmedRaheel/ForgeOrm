import React from 'react';
import { createRoot } from 'react-dom/client';
import { Cpu, Database, Gauge, Layers, LineChart, Rocket, Search, Shield } from 'lucide-react';
import './styles.css';

const features = [
  ['MSIL Performance', 'Runtime IL materializers, generated parameter binders, cached metadata, sequential readers.', Cpu],
  ['Source Generators', 'Compile-time reader and binder registration for zero reflection hot paths.', Rocket],
  ['Enterprise Querying', 'Raw SQL, QueryAst, CTE, temp tables, pivot, group by, having, page, stream.', Database],
  ['Bulk + Graph', 'Bulk insert/update/delete/merge hooks and graph insert/update/delete extension points.', Layers],
  ['AI + Vector Search', 'RAG-ready vector search, embeddings pipeline, dataframe-style analytics and AI query helpers.', Search],
  ['Provider Optimized', 'SQL Server, PostgreSQL, MySQL, Oracle and SQLite provider strategy extension points.', Shield]
];

const snippets = [
  ['Fast query', `var rows = await db.QueryAsync<Order>(\n    "select Id, OrderNo, GrandTotal from Orders where CustomerId=@CustomerId",\n    new { CustomerId = 10 }, ct);`],
  ['Streaming', `await foreach (var order in db.StreamAsync<Order>(sql, new { Status = "Paid" }, ct))\n{\n    // sequential reader, low allocation\n}`],
  ['Seek paging', `var page = await db.SeekPageAsync<Order, int>(\n    after: 5000, take: 100, orderBy: x => x.Id, ct);`],
  ['Vector search', `var results = await vector.SearchAsync(\n    queryVector, topK: 5, cancellationToken: ct);`]
];

function App() {
  return <main>
    <section className="hero">
      <nav><strong>ForgeORM</strong><span>Docs</span><span>Benchmarks</span><span>Samples</span><span>AI</span></nav>
      <div className="heroGrid">
        <div>
          <p className="eyebrow">Enterprise micro-ORM for modern .NET</p>
          <h1>Near-Dapper speed with EF-style productivity and enterprise features.</h1>
          <p className="lead">ForgeORM now includes MSIL materialization, source-generator hooks, cached metadata, bulk and graph extension points, temporal helpers, vector search, QueryAst and production-ready samples.</p>
          <div className="actions"><a href="#quickstart">Quick start</a><a href="#benchmarks" className="secondary">View benchmarks</a></div>
        </div>
        <div className="scoreCard">
          <Gauge size={42}/>
          <h2>Zero reflection hot path</h2>
          <p>Reflection is only used during cache build. Calls run through generated delegates, MSIL emit, concurrent dictionaries and sequential readers.</p>
        </div>
      </div>
    </section>

    <section className="section"><h2>What is included</h2><div className="cards">{features.map(([title, text, Icon]: any) => <article className="card" key={title}><Icon/><h3>{title}</h3><p>{text}</p></article>)}</div></section>

    <section id="quickstart" className="section split"><div><h2>Quick start</h2><p>Install ForgeORM packages, register the provider, then query with raw SQL, QueryAst or enterprise helpers.</p><pre>{`builder.Services.AddForgeOrm(o => o.UseSqlServer(connectionString));\n\nvar orders = await db.QueryAsync<Order>(sql, new { CustomerId = 10 });`}</pre></div><div><h2>Stable API layers</h2><ul><li>ForgeORM.Abstractions for contracts and attributes.</li><li>ForgeORM.Core for query, CRUD, stream, page and temporal operations.</li><li>Provider packages for database-specific optimization.</li><li>AI, VectorSearch, DataFrame and QueryAst as optional advanced modules.</li></ul></div></section>

    <section className="section"><h2>Examples</h2><div className="snippets">{snippets.map(([title, code]) => <article className="snippet" key={title}><h3>{title}</h3><pre>{code}</pre></article>)}</div></section>

    <section id="benchmarks" className="section metrics"><h2>Benchmark message</h2><div><LineChart/><p>Run <code>dotnet run -c Release --project benchmarks/ForgeORM.Benchmarks</code> to compare reflection baseline, ForgeORM MSIL materialization, binder overhead, Dapper and EF integration scenarios.</p></div></section>
  </main>
}

createRoot(document.getElementById('root')!).render(<App/>);
