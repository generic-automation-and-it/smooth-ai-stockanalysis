# LADR-016: lower_snake_case relational naming via EFCore.NamingConventions

**Status:** Accepted
**Date:** July 2026

## Context

SQLite identifiers are case-insensitive, but every surrounding tool (raw SQL in tests, `sqlite3` inspection, `PRAGMA` introspection, future hand-written queries) reads physical names verbatim. Without a global convention, EF Core's default naming bakes PascalCase CLR names into tables and columns, and each feature author must repeat `ToTable`/`HasColumnName` configuration by hand — the first production migration (`InitialUserSchema`, #60/#61/#65) already carried that explicit per-property naming. The choice (#259, under #7) had to be made while the schema is young, before more entities accumulate PascalCase names that later migrations must rename.

The convention must cover tables, columns, primary/foreign keys, and indexes globally, compose with the LADR-014 NodaTime converters and the LADR-015 document converter (both value-level mappings, orthogonal to naming), and require zero per-entity naming configuration from feature authors.

## Decision

Adopt `lower_snake_case` as the global relational naming standard, implemented with the `EFCore.NamingConventions` package's `UseSnakeCaseNamingConvention()` on the `DbContextOptionsBuilder`.

- The convention is applied at every production options seam: the Host DI registration (`AddInfrastructure`) and the design-time factory used by `dotnet ef`. Derived test probe contexts opt in on their own options builder.
- Feature authors must not repeat `ToTable`, `HasColumnName`, `HasName`, or `HasDatabaseName` for names the convention derives. Entity configuration classes carry only non-naming concerns (value generation, requiredness, column types, converters/comparers).
- A `DbSet` exposed through a set property is named after the set property (EF Core's set-name rule), so set properties should be named for the intended table (e.g. `Users`, `TimeRoundTripRecords`).
- The convention rewrites EF's infrastructure names too: the migrations-history columns become `migration_id`/`product_version` (the table stays `__EFMigrationsHistory`).
- The JSON *payload* contract inside document columns (`SqliteJsonSerialization.Default`, LADR-015) is untouched: camelCase inside the stored value, snake_case for the relational names around it.

## Alternatives considered

**Hand-rolled `IModelFinalizingConvention` rewriting table/column/key/index names.** Rejected: zero added dependencies, but the team would own the edge cases the package already handles (composite keys, index names, shadow properties, set-name rules, future EF versions). The single well-maintained dependency is the lower-risk, more complete option; the hand-rolled NodaTime converters exist because no maintained package covers lossless NodaTime-on-SQLite, which is not true here.

**Per-entity explicit names (status quo of `InitialUserSchema`).** Rejected: repetitive, easy to drift, and pushes a global cross-cutting decision into every feature's configuration class.

## Consequences

- Every future entity — including all user-owned tables under #7 — inherits snake_case table, column, key, and index names at zero per-entity cost, and composite ownership-unique indexes `(user_id, ...)` follow the same convention.
- The pre-release `users` table was renamed to the convention-derived `user_record` (from the `UserRecord` persistence type) through the `SnakeCaseNamingConvention` migration, and the unique index became `ix_user_record_unique_identifier`. Accepted because no production database exists yet; the renames keep the schema convention-pure instead of freezing first-migration names as exceptions.
- Raw SQL in tests and operational scripts must use physical snake_case names.
- One new centrally-versioned dependency (`EFCore.NamingConventions`) is pinned in `Directory.Packages.props` and referenced only by Infrastructure.
