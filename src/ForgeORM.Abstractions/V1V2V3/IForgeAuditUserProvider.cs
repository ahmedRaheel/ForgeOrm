namespace ForgeORM.Abstractions;

public interface IForgeAuditUserProvider
{
    string? UserId { get; }
    string? UserName { get; }
}
