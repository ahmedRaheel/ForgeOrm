namespace ForgeORM.RuntimeEmit;

public static class ForgeRuntimeEmitMode
{
    public static void UseRuntimeEmit()
    {
        ForgeORM.Core.ForgeOrmCompilationRuntime.Mode = ForgeORM.Core.ForgeOrmCompilationMode.RuntimeEmit;
    }
}
