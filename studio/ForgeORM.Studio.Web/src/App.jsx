import React, { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Database, Activity, Shield, Search, Wand2 } from 'lucide-react';
import './styles.css';

const apiBase = import.meta.env.VITE_FORGEORM_STUDIO_API || 'http://localhost:5000';

function App() {
  const [sql, setSql] = useState('SELECT * FROM dbo.Products p LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId');
  const [result, setResult] = useState(null);

  async function visualize() {
    const res = await fetch(`${apiBase}/studio/query/visualize`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sql, provider: 'SqlServer' }) });
    setResult(await res.json());
  }

  async function optimize() {
    const res = await fetch(`${apiBase}/studio/ai/optimize`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sql, provider: 'SqlServer' }) });
    setResult(await res.json());
  }

  return <main className="shell">
    <aside className="sidebar">
      <h1>ForgeORM Studio</h1>
      <button><Database/> ERD Designer</button>
      <button><Search/> Query Visualizer</button>
      <button><Activity/> Monitoring</button>
      <button><Shield/> Security</button>
      <button><Wand2/> AI Tools</button>
    </aside>
    <section className="workspace">
      <div className="hero"><h2>AI-first Enterprise ORM Control Center</h2><p>Visualize SQL, run diagnostics, manage SaaS tenants, test APIs and prepare vector search scenarios.</p></div>
      <textarea value={sql} onChange={e => setSql(e.target.value)} />
      <div className="actions"><button onClick={visualize}>Visualize Query</button><button onClick={optimize}>AI Optimize</button></div>
      <pre>{result ? JSON.stringify(result, null, 2) : 'Result will appear here...'}</pre>
    </section>
  
      <section className="card"><h2>RAG Engine</h2><p>Ingest documents, chunk content, generate embeddings and retrieve context for AI answers.</p></section>
      <section className="card"><h2>Workflow Designer</h2><p>Visual nodes for APIs, queues, database actions, approvals, AI actions and saga compensations.</p></section>
      <section className="card"><h2>AI Agents</h2><p>Optimization, diagnostics, reporting, migration, security and observability agents.</p></section>
      <section className="card"><h2>Low-Code ERP Generator</h2><p>Generate entities, APIs, forms, dashboards, reports and workflow modules from metadata.</p></section>
      <section className="card"><h2>Cloud + IaC Generator</h2><p>Generate Dockerfile, Kubernetes YAML, Helm values and Terraform starter files.</p></section>
      <section className="card"><h2>Marketplace</h2><p>Publish providers, templates, AI agents, workflow packs, reports and ERP modules.</p></section>
    </main>
}

createRoot(document.getElementById('root')).render(<App />);
