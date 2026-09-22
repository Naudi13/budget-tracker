# ADR-002 — Angular, .NET and PostgreSQL as the Technology Stack

## Identification

| Field | Value |
|---|---|
| Id | ADR-002 |
| Date | 2026-09-21 |
| Status | Proposed |
| Authors | Tech lead and team, at the architecture review gate |
| Supersedes | none |

## Context

The folder tree, the boundary tool, the migration library and the CI stages all
follow from the language and the database, so the stack is fixed before the
first commit. None of the four guides in `_stacks/` is written for C#.

| Constraint | Source |
|---|---|
| Source lives in `bud-tack-front`, `bud-tack-api`, `bud-tack-db` and `bud-tack-docs`, one per piece, an exception recorded in [`ADR-004`](./ADR-004-shared-database-and-per-piece-repositories.md) | [`../../../00-governance/git-conventions.md`](../../../00-governance/git-conventions.md) |
| One relational database shared by all modules, consistent within one transaction — an exception recorded in `ADR-004` | [`../../../01-context/overview.md`](../../../01-context/overview.md) |
| A developer reaches a first successful request in under 1 hour | `NFR-08` |
| The team works in C# and Angular | The team |

## Decision

**We decided:** build the frontend in Angular, the backend as one ASP.NET Core
application in C#, and store data in PostgreSQL.

The tiebreaker was the team's existing skill: with C# and Angular already known,
the first request is reachable inside the hour `NFR-08` allows, and the runner-up
had the advantage of a ready guide but not of the team's experience.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| Angular, ASP.NET Core, PostgreSQL | Matches the team's skills; PostgreSQL is the database `.env.example` already assumes | No `_stacks/` guide for C#; the tree, boundary tool and commands are written by this project | Chosen |
| Angular, Spring Boot, PostgreSQL | A ready `_stacks/java-spring.md` guide with the boundary test written | The team's backend language is C#, not Java | The guide's head start does not offset learning a language |
| Angular, ASP.NET Core, a document database | Flexible schema | Totals and their detail lines must stay consistent, and [`BR-05`](../../../02-domain/entities-and-rules.md) relies on restrict-on-delete keys | Ruled out by the relational constraint in `overview.md` |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | No ramp-up: the team starts writing modules on day one |
| Positive | Foreign keys with restrict-on-delete enforce `BR-05` in the data as well as in code |
| Cost | A C# guide in `_stacks/`, with its version baseline, must be written; the three unused guides stay until then |
| Risk | Two toolchains, .NET and Angular's, in two repositories; the signal is the timed `NFR-08` run exceeding 1 hour |

**Documents to update:** `_stacks/` (a C# guide, and removal of the unused
guides), [`../../../06-data/migrations.md`](../../../06-data/migrations.md),
[`../../../10-devops/ci-cd.md`](../../../10-devops/ci-cd.md).

## Revisiting

Reopened if the timed `NFR-08` onboarding run exceeds 1 hour twice in a row, or
if a library the application needs has no release for the .NET line in use.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-003-project-per-module-boundary.md`](./ADR-003-project-per-module-boundary.md)
