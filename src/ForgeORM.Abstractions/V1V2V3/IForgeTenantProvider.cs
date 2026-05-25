namespace ForgeORM.Abstractions;

public interface IForgeTenantProvider
{
    ForgeTenantContext Current { get; }
}
