# ADR-001 — Modular Monolith as the Default Architecture

## Identification

| Field | Value |
|---|---|
| Id | ADR-001 |
| Date | 2026-11-04 |
| Status | Accepted |
| Authors | Tech lead and team, at the architecture review gate |
| Supersedes | none |

## Context

A team must know, before the first commit, whether code is organized by
business area or by technical layer. The decision is cheap now and expensive
later: the folder tree, the test layout, the build check and every import in
the codebase follow from it, and changing it means moving files that several
people are editing at once.

| Constraint | Source |
|---|---|
| One deployable, one shared database; source in five repositories released as a unit — an exception to the course standard, recorded in [`ADR-004`](./ADR-004-shared-database-and-per-piece-repositories.md) | [`../../../01-context/overview.md`](../../../01-context/overview.md) |
| Three bounded contexts already exist: identity, transactions and dashboard | [`../../../02-domain/domain-map.md`](../../../02-domain/domain-map.md) |
| A small team, no dedicated operations hire | [`../../../03-product/vision.md`](../../../03-product/vision.md) |
| A developer reaches a first successful request in under 1 hour | `NFR-08` |

## Decision

**We decided:** organize the application as a **modular monolith** — one
deployable divided into modules, each with a public API and internals nothing
outside may import — and keep **flat layered architecture** as a documented
variant for a project whose domain map holds a single bounded context.

The tiebreaker was the domain map, not size. Three contexts were already drawn
there, each owning its own data or, for dashboard, reading another module's, and
the boundary is far cheaper to draw before the code exists than after.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| Modular monolith by default, flat layers as a documented variant | Boundary matches the domain map; blast radius of a change is one folder; the rule is machine-checkable | Two documented shapes to teach instead of one | Chosen |
| Flat layered architecture, for every project | Simplest tree; fastest start; no extra tooling | With three contexts, one file becomes the junction every feature edits, and business areas blur into each other | Kept as the variant below the threshold, rejected here |
| Modules with enforced boundaries from day one, on every project | One rule everywhere; nothing to migrate later | Draws boundaries before the domain is understood; the ceremony lands hardest on the smallest projects | Rejected |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | A change to transactions stays inside one folder, so a reviewer sees its reach in the diff |
| Positive | The boundary is checked by the build, not remembered by a reviewer |
| Cost | One rule file and one `boundary check` stage per project to maintain |
| Cost | A module's public API is a real interface; widening it is a review conversation, not a quick edit |
| Risk | Modules drawn too small become layers with extra ceremony — the warning sign is a facade with twenty methods |

**Documents to update:** none; this record is why
[`../../modular-monolith.md`](../../modular-monolith.md) and
[`../../layered-architecture.md`](../../layered-architecture.md) exist.

## Revisiting

Reopened when a module's public API is widened three times in one quarter —
the sign the boundary sits in the wrong place. The growth path this decision
sets runs from the variant to the default and ends there; what lies beyond the
edge of that path is listed in [`../../README.md`](../../README.md).

---

**Related:** [`../README.md`](../README.md) · [`../../modular-monolith.md`](../../modular-monolith.md)
