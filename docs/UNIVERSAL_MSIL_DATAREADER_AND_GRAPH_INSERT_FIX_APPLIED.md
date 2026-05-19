# Universal MSIL/DataReader + Graph Insert Fix Applied

Applied fixes:
- ForgeAdo query methods continue to use DbDataReader + cached Reflection.Emit materializers.
- ForgeGridReader multiple result reads now use ForgeIlMaterializerCache instead of ForgeMaterializer.Map.
- Runtime-type graph split queries now use ForgeIlMaterializerCache.GetOrCreate(Type, reader) instead of Activator/PropertyInfo.SetValue mapping.
- Stored procedure single/single-or-default methods now use direct reader single-row execution instead of materializing a list and calling SingleOrDefault.
- Dynamic dictionary query uses SequentialAccess and pre-sized lists.
- Graph child row inserts now exclude numeric identity keys, read SCOPE_IDENTITY, and write generated keys back to child entities.
- Fixed duplicated local variable in SQL Server TVP parameter configurator.
