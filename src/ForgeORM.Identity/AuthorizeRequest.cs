using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public sealed record AuthorizeRequest
{
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string UserId { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string Resource { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string Action { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public string? TenantId { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public IReadOnlyList<string> Roles { get; init; } = [];
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public string? IpAddress { get; init; }
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public string? UserAgent { get; init; }
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public ForgePrincipal ToPrincipal() => new(UserId, Roles, Claims);
    /// <summary>
    /// Executes the ToRequirement operation.
    /// </summary>
    /// <returns>The result of the ToRequirement operation.</returns>
    public ForgePolicyRequirement ToRequirement() => new(Resource, Action, TenantId);
}
