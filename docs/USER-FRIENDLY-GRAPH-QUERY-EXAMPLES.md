# ForgeORM User-Friendly API Examples

This sample API exposes Swagger endpoints under `/examples/user-friendly/*` that document the intended easy API shape for:

- single entity insert
- graph insert with all child collections
- graph update with child upsert
- delete parent only, children only, or parent with children
- SQL-first QueryAst join/projection
- expression-first QueryAst join/projection
- split graph one-to-many with projection
- bulk ids and condition helpers

These examples are intentionally grouped separately from the older working sample endpoints so users can compare the old verbose style with the newer user-friendly style.
