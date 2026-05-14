import React, { useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Activity, Bot, Boxes, BrainCircuit, Database, GitBranch, KeyRound, Network, Play, Radio, Search, Shield, Store, Workflow, Zap } from 'lucide-react';
import './styles.css';

const apiBase = import.meta.env.VITE_FORGEORM_STUDIO_API || 'http://localhost:5000';

const panels = [
  ['query', 'Query Visualizer', Search],
  ['erd', 'ERD Designer', Database],
  ['monitoring', 'Monitoring', Activity],
  ['ai', 'AI Tools', Bot],
  ['rag', 'RAG', BrainCircuit],
  ['workflow', 'Workflow', Workflow],
  ['events', 'Event Sourcing', Zap],
  ['realtime', 'Realtime', Radio],
  ['lowcode', 'Low-Code ERP', Boxes],
  ['federated', 'Distributed Queries', Network],
  ['security', 'Security', Shield],
  ['identity', 'Identity', KeyRound],
  ['sync', 'Offline Sync', GitBranch],
  ['marketplace', 'Marketplace', Store],
  ['api', 'API Tester', Play]
];

async function post(path, body) {
  const res = await fetch(`${apiBase}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  });
  return res.json();
}

async function get(path) {
  const res = await fetch(`${apiBase}${path}`);
  return res.json();
}

function Result({ value }) {
  return <pre className="result">{value ? JSON.stringify(value, null, 2) : 'Run an action to see the response...'}</pre>;
}

function App() {
  const [active, setActive] = useState('query');
  const [sql, setSql] = useState('SELECT * FROM dbo.Products p LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId');
  const [question, setQuestion] = useState('What modules exist in ForgeORM?');
  const [result, setResult] = useState(null);

  const ActiveIcon = useMemo(() => panels.find(x => x[0] === active)?.[2] || Search, [active]);

  async function runAction(kind) {
    if (kind === 'query') return setResult(await post('/studio/query/visualize', { sql, provider: 'SqlServer' }));
    if (kind === 'optimize') return setResult(await post('/studio/ai/optimize', { sql, provider: 'SqlServer' }));
    if (kind === 'erd') return setResult(await get('/studio/erd/sample'));
    if (kind === 'monitoring') return setResult(await get('/studio/monitoring'));
    if (kind === 'rag-ingest') return setResult(await post('/studio/rag/ingest', { id: 'wiki-1', title: 'ForgeORM Wiki', content: 'ForgeORM supports ORM, RAG, vector search, telemetry, workflow, distributed queries, Redis cache and AI diagnostics.' }));
    if (kind === 'rag-context') return setResult(await post('/studio/rag/context', { question, topK: 5 }));
    if (kind === 'workflow') return setResult(await post('/studio/workflows/designer', { name: 'Order Approval', steps: [{ name: 'Validate' }, { name: 'Approve' }, { name: 'Complete' }] }));
    if (kind === 'event') return setResult(await post('/studio/events/append', { streamId: 'studio-demo', eventType: 'QueryExecuted', data: { sql }, userId: 'admin' }));
    if (kind === 'event-read') return setResult(await get('/studio/events/studio-demo'));
    if (kind === 'realtime') return setResult(await post('/studio/realtime/publish', { topic: 'studio', eventName: 'Ping', payload: { message: 'Hello Studio' }, timestampUtc: new Date().toISOString() }));
    if (kind === 'erp') return setResult(await post('/studio/lowcode/erp', { companyName: 'Contoso', industry: 'Manufacturing', modules: ['Inventory', 'Sales', 'Finance', 'HR'] }));
    if (kind === 'federated') return setResult(await post('/studio/federated/plan', { query: 'Get customer sales summary', sources: [{ name: 'sales-sql', type: 1, database: 'SalesDb' }, { name: 'reporting-pg', type: 2, database: 'ReportingDb' }, { name: 'crm-api', type: 9 }] }));
    if (kind === 'identity') return setResult(await post('/studio/identity/authorize', { userId: 'admin', resource: 'orders', action: 'approve', roles: ['Admin'], claims: { department: 'Finance' } }));
    if (kind === 'sync') return setResult(await post('/studio/sync', { deviceId: 'device-1', tenantId: 'tenant-1', userId: 'user-1', lastSyncAt: new Date().toISOString(), entities: [{ entityName: 'Orders', localChanges: [{ id: 1, status: 'Completed' }] }] }));
    if (kind === 'marketplace') return setResult(await get('/studio/marketplace?q=&category='));
    if (kind === 'api') return setResult(await post('/studio/api-test', { method: 'GET', url: `${apiBase}/`, headers: {}, body: '' }));
  }

  return <main className="shell">
    <aside className="sidebar">
      <h1>ForgeORM Studio</h1>
      <p className="tagline">AI Enterprise Data Platform</p>
      <nav>
        {panels.map(([id, label, Icon]) => <button key={id} className={active === id ? 'active' : ''} onClick={() => { setActive(id); setResult(null); }}><Icon size={18}/> {label}</button>)}
      </nav>
    </aside>

    <section className="workspace">
      <div className="hero">
        <div><ActiveIcon size={34}/></div>
        <div><h2>{panels.find(x => x[0] === active)?.[1]}</h2><p>Runnable ForgeORM Studio portal connected to the Studio API endpoints.</p></div>
      </div>

      {active === 'query' && <section className="card"><h3>SQL Query Visualizer</h3><textarea value={sql} onChange={e => setSql(e.target.value)} /><div className="actions"><button onClick={() => runAction('query')}>Visualize Query</button><button onClick={() => runAction('optimize')}>AI Optimize</button></div></section>}
      {active === 'erd' && <section className="card"><h3>ERD Designer</h3><button onClick={() => runAction('erd')}>Load Sample ERD</button></section>}
      {active === 'monitoring' && <section className="card"><h3>Monitoring Dashboard</h3><button onClick={() => runAction('monitoring')}>Load Metrics</button></section>}
      {active === 'ai' && <section className="card"><h3>AI Diagnostics</h3><textarea value={sql} onChange={e => setSql(e.target.value)} /><button onClick={() => runAction('optimize')}>Optimize SQL</button></section>}
      {active === 'rag' && <section className="card"><h3>RAG Engine</h3><input value={question} onChange={e => setQuestion(e.target.value)} /><div className="actions"><button onClick={() => runAction('rag-ingest')}>Ingest Demo Document</button><button onClick={() => runAction('rag-context')}>Build Context</button></div></section>}
      {active === 'workflow' && <section className="card"><h3>Workflow Designer</h3><button onClick={() => runAction('workflow')}>Generate Workflow Graph</button></section>}
      {active === 'events' && <section className="card"><h3>Event Sourcing</h3><div className="actions"><button onClick={() => runAction('event')}>Append Studio Event</button><button onClick={() => runAction('event-read')}>Read Stream</button></div></section>}
      {active === 'realtime' && <section className="card"><h3>Realtime</h3><button onClick={() => runAction('realtime')}>Publish Event</button></section>}
      {active === 'lowcode' && <section className="card"><h3>Low-Code ERP Generator</h3><button onClick={() => runAction('erp')}>Generate ERP Metadata</button></section>}
      {active === 'federated' && <section className="card"><h3>Distributed Queries</h3><button onClick={() => runAction('federated')}>Build Federated Plan</button></section>}
      {active === 'identity' && <section className="card"><h3>Identity Policy Engine</h3><button onClick={() => runAction('identity')}>Authorize Demo User</button></section>}
      {active === 'sync' && <section className="card"><h3>Offline Sync</h3><button onClick={() => runAction('sync')}>Run Sync</button></section>}
      {active === 'marketplace' && <section className="card"><h3>Marketplace</h3><button onClick={() => runAction('marketplace')}>Search Marketplace</button></section>}
      {active === 'api' && <section className="card"><h3>API Tester</h3><button onClick={() => runAction('api')}>Call Studio API Home</button></section>}
      {active === 'security' && <section className="card"><h3>Security</h3><p>Use AI diagnostics, policy engine, SQL validator, encryption, masking and tenant-aware authorization endpoints.</p><button onClick={() => runAction('identity')}>Run Authorization Check</button></section>}

      <Result value={result}/>
    </section>
  </main>;
}

createRoot(document.getElementById('root')).render(<App />);
