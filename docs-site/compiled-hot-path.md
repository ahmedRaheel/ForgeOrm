# Compiled Hot Path

`ForgeCompiledPlanCache` compiles property getters/setters and SQL plans once, then reuses them to avoid reflection in hot paths.
