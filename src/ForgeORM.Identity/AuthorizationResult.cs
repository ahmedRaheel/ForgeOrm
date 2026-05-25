using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public sealed record AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string? Policy { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<string> MissingRoles { get; init; } = [];
    public IReadOnlyList<string> MissingClaims { get; init; } = [];
}
