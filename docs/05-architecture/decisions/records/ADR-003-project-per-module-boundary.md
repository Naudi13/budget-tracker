# ADR-003 — One Project per Module, Checked by the Compiler and NetArchTest

## Identification

| Field | Value |
|---|---|
| Id | ADR-003 |
| Date | 2026-09-21 |
| Status | Proposed |
| Authors | Tech lead and team, at the architecture review gate |
| Supersedes | none |

## Context

`ADR-001` requires modules whose internals nothing outside may import, and this
section requires a `boundary check` that fails the pull request on a violation.
C# is outside the tools the framework lists, so the mechanism is chosen here.

| Constraint | Source |
|---|---|
| Three modules: identity, transactions and dashboard | [`../../../02-domain/module-boundaries.md`](../../../02-domain/module-boundaries.md) |
| A boundary violation fails the pull request | [`../../README.md`](../../README.md) |
| Application code never imports web-framework types | [`../../layered-architecture.md`](../../layered-architecture.md) |

## Decision

**We decided:** each module is one C# class library project whose only `public`
types are its facade and contracts, everything else `internal`; the HTTP
endpoints live in the host project, calling facades only; and a NetArchTest
suite fails the build when an `Internal` type is public or depended on from
another module.

The tiebreaker was that the compiler already refuses access to `internal` types
across projects, so the test only covers what the compiler cannot: a type made
`public` by mistake.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| Project per module, `internal`, NetArchTest | Two lines of defence; the compiler does most of the work | More projects and one marker type per module | Chosen |
| One project, a folder per module, NetArchTest only | Fewer projects | A single line of defence, and nothing stops a `public` internal except the test | Ruled out: a test edit alone lifts the rule |
| One project per module with ArchUnitNET instead | Closest to the ArchUnit used in the Java guide | A second API to learn | NetArchTest already expresses the one rule enforced here |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | A cross-module reference to an internal type fails before any test runs |
| Positive | Endpoints outside the modules keep web-framework types out of the application layer |
| Cost | Each module needs its own project and one empty marker type |
| Risk | An `InternalsVisibleTo` grant to another module's assembly; the signal is one appearing without an ADR |

**Documents to update:** [`../../module-structure.md`](../../module-structure.md),
[`../../boundary-enforcement.md`](../../boundary-enforcement.md),
[`../../../10-devops/ci-cd.md`](../../../10-devops/ci-cd.md) (the `boundary check` stage).

## Revisiting

Reopened the first time an `InternalsVisibleTo` grant to another module's
assembly is proposed, since that means the boundary sits in the wrong place.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-001-modular-monolith-as-default.md`](./ADR-001-modular-monolith-as-default.md)
