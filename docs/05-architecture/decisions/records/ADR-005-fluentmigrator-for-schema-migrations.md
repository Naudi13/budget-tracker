# ADR-005 — FluentMigrator for Schema Migrations, in `bud-tack-db`

## Identification

| Field | Value |
|---|---|
| Id | ADR-005 |
| Date | 2026-11-04 |
| Status | Proposed |
| Authors | Tech lead and team, at the architecture review gate |
| Supersedes | none |

## Context

[`migrations.md`](../../../06-data/migrations.md) named Liquibase, which runs on a
Java runtime: the C# guide had to call its standalone CLI because no .NET-native
package exists. [`ADR-002`](./ADR-002-angular-dotnet-postgresql-stack.md) fixes the
stack at Angular, .NET and PostgreSQL, so a Java runtime on every developer machine
and CI runner is a fourth toolchain the stack does not allow.

| Constraint | Source |
|---|---|
| Only Angular, .NET and PostgreSQL toolchains; no Java runtime anywhere | `ADR-002`, the team |
| One ordered sequence for the whole database, never one per module | [`migrations.md`](../../../06-data/migrations.md) |
| Every change has a working rollback, verified in CI before merge | `migrations.md`, DoD #5 |
| Schema, seeds and migrations live in their own repository, `bud-tack-db` | [`ADR-004`](./ADR-004-shared-database-and-per-piece-repositories.md) |
| Migrations run under a privileged role the application never uses | [`security-policy.md`](../../../00-governance/security-policy.md) |
| A developer reaches a first successful request in under 1 hour | `NFR-08` |

## Decision

**We decided:** version the schema with **FluentMigrator**, in a .NET console
project `BudTack.Db.Migrations` inside `bud-tack-db`, with one version table for
the whole database; `bud-tack-api` holds no migrations.

The tiebreaker was rollback: FluentMigrator gives every migration a `Down()`
(automatic for additive changes through `AutoReversingMigration`), which is what
`migrations.md` requires of every change. DbUp is simpler, but has no rollback.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| FluentMigrator, own console project in `bud-tack-db` | Pure .NET; one version table; `Up()`/`Down()` per migration; schema-aware (`InSchema`) | Migrations are C# classes, not plain SQL; raw SQL goes through `Execute.Sql` | Chosen |
| Liquibase CLI | The framework's original choice; mature | Needs a Java runtime | Ruled out by the no-Java constraint |
| EF Core Migrations | Ships with EF Core; the team knows it | One history per `DbContext`, so one per module; tied to the model in `bud-tack-api` | Ruled out by the one-sequence rule and by `ADR-004` |
| DbUp, plain `.sql` scripts | Simplest; SQL-first | No rollback at all | Ruled out by the rollback rule |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | One SDK, `dotnet`, builds the API, the tests and the migrations |
| Positive | CI proves each rollback by migrating up, down to zero, and up again |
| Cost | `bud-tack-db` gets its own .NET project, pipeline and `.env.example` |
| Cost | The schema is not in `bud-tack-api`'s tree; a story changing a table opens a pull request in both, `bud-tack-db` first |
| Risk | A migration whose `Down()` does not undo its `Up()`; the signal is the CI round trip failing or leaving a diff |

**Documents to update:** [`../../../06-data/migrations.md`](../../../06-data/migrations.md),
[`../../../06-data/README.md`](../../../06-data/README.md),
[`../../module-structure.md`](../../module-structure.md),
[`../../../_stacks/csharp-aspnet.md`](../../../_stacks/csharp-aspnet.md),
[`../../../10-devops/local-setup.md`](../../../10-devops/local-setup.md),
[`../../../10-devops/ci-cd.md`](../../../10-devops/ci-cd.md).

## Revisiting

Reopened if FluentMigrator publishes no release supporting the .NET line in use
within six months of that line's release, or if the CI round trip fails twice in
one sprint for a reason the tool, not the migration, caused.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-002-angular-dotnet-postgresql-stack.md`](./ADR-002-angular-dotnet-postgresql-stack.md)
