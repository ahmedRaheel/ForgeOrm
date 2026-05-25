using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public static class ForgePlatform
{
    public static IReadOnlyList<ForgeModuleDescriptor> Modules { get; } =
    [
        new("V1 Core ORM", ForgeReleasePhase.V1Core,
        [
            Feature("V1-001", "Hybrid ORM engine", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Raw SQL, stored procedures, repository helpers and query builders in one API."),
            Feature("V1-002", "Expression query builder", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Expression-based filters, sorting and paging."),
            Feature("V1-003", "Provider abstraction", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Provider-oriented SQL rendering and dialect support."),
            Feature("V1-004", "Compiled query cache", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Caches generated SQL shapes for repeated execution.")
        ]),
        new("V2 Enterprise", ForgeReleasePhase.V2Enterprise,
        [
            Feature("V2-001", "Bulk operations", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Bulk insert, update, delete and merge extension points."),
            Feature("V2-002", "Multi-tenancy", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Tenant context for shared DB, schema-per-tenant or database-per-tenant strategies."),
            Feature("V2-003", "Auditing and soft delete", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Created/updated user stamps and soft delete helpers."),
            Feature("V2-004", "Outbox pattern", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Preview, "In-memory outbox store plus database integration contract."),
            Feature("V2-005", "Reporting engine", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Dynamic SQL report builder for dashboards, pivots and exports."),
            Feature("V2-006", "Caching", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Cache provider contract and in-memory implementation.")
        ]),
        new("V3 AI First", ForgeReleasePhase.V3AiFirst,
        [
            Feature("V3-001", "Natural language to SQL", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.Preview, "AI query client abstraction with safe deterministic fallback."),
            Feature("V3-002", "AI query diagnostics", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.Preview, "Warnings, index hints and explanation generation."),
            Feature("V3-003", "AI CRUD API generation", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.Preview, "Generates Minimal API endpoints for a selected entity."),
            Feature("V3-004", "AI migration planning", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.ExtensionPoint, "Schema diff and migration plan extension point."),
            Feature("V3-005", "Scaffolding", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.ExtensionPoint, "Reverse engineering extension point for database-first adoption.")
        ])
    ];

    public static IReadOnlyList<ForgeFeatureDescriptor> Features =>
        Modules.SelectMany(x => x.Features).ToList();

    private static ForgeFeatureDescriptor Feature(string code, string name, ForgeReleasePhase phase, ForgeFeatureStatus status, string description)
        => new(code, name, phase, status, description);
}
