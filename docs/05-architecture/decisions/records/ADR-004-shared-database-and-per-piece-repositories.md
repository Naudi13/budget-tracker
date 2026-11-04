# ADR-004 — One Shared Database and One Repository per Piece

## Identification

| Field | Value |
|---|---|
| Id | ADR-004 |
| Date | 2026-11-04 |
| Status | Accepted |
| Authors | Tech lead and team, at the architecture review gate |
| Supersedes | none |

## Context

The course standard gives each domain its own database in its own instance, and
names repositories `<abbr>-<domain>-<piece>`. This project is a modular monolith
([`ADR-001`](./ADR-001-modular-monolith-as-default.md)): three bounded contexts
in one deployable. Applying the standard literally would mean one database
instance and one repository set per domain. This record states the deviation
instead of leaving it implicit in a diagram.

| Constraint | Source |
|---|---|
| One relational database; income, expenses and balance stay consistent within one transaction | [`../../../01-context/overview.md`](../../../01-context/overview.md) |
| One deployable, released as a unit | [`../../../01-context/overview.md`](../../../01-context/overview.md) |
| Dashboard owns no tables and reads transactions in-process | [`../../../02-domain/module-boundaries.md`](../../../02-domain/module-boundaries.md) |
| Every table has exactly one owning module | [`../../../02-domain/module-boundaries.md`](../../../02-domain/module-boundaries.md) |
| One restore drill for the database, under 30 minutes | `NFR-04` |

## Decision

**We decided:** treat this project as a documented exception to the per-domain
database and per-domain repository rules: one PostgreSQL instance with one schema
per table-owning module (`identity`, `transactions`), and repositories named per
piece — `bud-tack-api`, `bud-tack-portal`, `bud-tack-db`, `bud-tack-infra` — with no domain segment.

The tiebreaker was that the modules are not deployed apart: `dashboard` reads
`transactions` inside one process, and a database per domain would turn that read
into a call between instances, which this section keeps out of scope.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| One instance, a schema per owning module; repositories per piece | One transaction spans the balance and its inputs; one backup and one restore | Deviates from the course standard; isolation rests on the boundary check, not on separate instances | Chosen |
| One database instance per domain, as the standard says | Full isolation of each domain's data | A balance computed across instances is no longer one transaction, and there are three restores to drill | Ruled out by the one-database constraint |
| One database, one shared schema, no owners | Simplest to set up | Any module could write any table, which `module-boundaries.md` forbids | Ruled out by the single-owner rule |
| One repository set per domain | Matches the naming standard | Splits the source of one deployable across three backends | Ruled out: the modules ship in one build |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | Balance and totals are read consistently in one transaction (`BR-03`) |
| Positive | One backup and one restore drill cover the whole application |
| Cost | A deviation from the course standard that this record must defend at the gate |
| Risk | A module writing another module's table; the signal is a cross-schema write found in review, such as a foreign key from `created_by` to `users` |

**Documents to update:** [`../../modular-monolith.md`](../../modular-monolith.md),
and `06-data`, which must name the schemas and settle the cross-module keys.

## Revisiting

Reopened if the architecture gate rejects the exception, or if a module needs its
own release cadence and so stops shipping in the one build.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-002-angular-dotnet-postgresql-stack.md`](./ADR-002-angular-dotnet-postgresql-stack.md)
