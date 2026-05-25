using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record GenerateErpRequest
{
    public required string CompanyName { get; init; }

    public required string Industry { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<string> Modules { get; init; }
        = [];

    public IReadOnlyList<string> Databases { get; init; }
        = [];

    public bool GenerateApis { get; init; } = true;

    public bool GenerateFrontend { get; init; } = true;

    public bool GenerateReports { get; init; } = true;

    public bool GenerateWorkflows { get; init; } = true;

    public bool GenerateMultiTenancy { get; init; } = true;

    public bool GenerateAuditing { get; init; } = true;

    public bool GeneratePermissions { get; init; } = true;

    public bool GenerateDocker { get; init; } = true;

    public bool GenerateKubernetes { get; init; } = true;

    public bool GenerateTerraform { get; init; } = true;

    public string? PreferredDatabase { get; init; }

    public string? PreferredFrontend { get; init; }

    public string? PreferredArchitecture { get; init; }

    public string? AiProvider { get; init; }

    public string? TenantId { get; init; }

    public string? UserId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
