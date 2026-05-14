# ForgeORM V4 Platform Modules Added

This update adds additive modules into the same tested base solution.

## Added Projects

- ForgeORM.Rag
- ForgeORM.Workflow
- ForgeORM.EventSourcing
- ForgeORM.Realtime
- ForgeORM.AI.Agents
- ForgeORM.LowCode
- ForgeORM.Cloud
- ForgeORM.Identity
- ForgeORM.Sync
- ForgeORM.Marketplace
- ForgeORM.DataVirtualization
- ForgeORM.TimeTravel
- ForgeORM.Observability.AI
- ForgeORM.AI.Memory

## Added Capabilities

- RAG ingestion, chunking, deterministic embeddings, vector retrieval and answer context generation
- Workflow orchestration plus visual workflow designer model
- CQRS/event sourcing primitives with event store and replay
- Realtime pub/sub abstraction for live dashboards
- AI agent runner and optimization agent
- Low-code metadata engine and ERP generator
- Cloud deployment and IaC artifact generator
- RBAC/ABAC policy engine
- Offline-first synchronization and conflict detection
- Marketplace catalog for templates/providers/agents/workflows/reports
- Distributed/federated query planner and data virtualization
- Time-travel SQL builder
- AI observability analyzer over telemetry snapshots
- AI memory store

## Studio API Endpoints

- POST /studio/rag/ingest
- POST /studio/rag/context
- POST /studio/workflows/run
- POST /studio/workflows/designer
- POST /studio/events/append
- GET  /studio/events/{streamId}
- POST /studio/realtime/publish
- POST /studio/agents/run
- POST /studio/lowcode/erp
- POST /studio/cloud/deployment
- POST /studio/identity/authorize
- POST /studio/sync
- POST /studio/marketplace/publish
- GET  /studio/marketplace
- POST /studio/federated/plan
- POST /studio/time-travel/sql
- GET  /studio/observability/ai
- POST /studio/memory/remember
- GET  /studio/memory/{scope}

## Note

These are functional enterprise-ready starter modules. Some areas such as production Redis Cluster, real SignalR transport, real AI provider calls, production distributed workflows and real cloud provisioning still need provider-specific adapters.
