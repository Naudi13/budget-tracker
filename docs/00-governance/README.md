# 00 — Governance

## Purpose

This section holds the rules the whole project commits to before any other
section is filled in: how work enters a sprint, how it is closed, how branches
and commits are named across the `bud-tack-docs`, `-api`, `-portal`, `-db`, and `-infra` repositories,
which ceremonies run, how documentation is written, and how secrets and
dependencies are kept safe inside a single deployable application.

## Documents

| Document | Answers | Priority |
|---|---|---|
| [definition-of-ready.md](./definition-of-ready.md) | When can a story enter a sprint? | ⭐ |
| [definition-of-done.md](./definition-of-done.md) | When is a story finished? | ⭐ |
| [git-conventions.md](./git-conventions.md) | How are branches and commits named? | ⭐ |
| [documentation-rules.md](./documentation-rules.md) | When is a document done? | ⭐ |
| [agile-conventions.md](./agile-conventions.md) | What ceremonies run, and what do they produce? | — |
| [security-policy.md](./security-policy.md) | How are secrets, dependencies and access protected? | — |
| [_template-sprint-retro.md](./_template-sprint-retro.md) | Template — copy it once per sprint | — |

## Out of scope

This section governs one deployable application built and released by one team,
whose source is split across five repositories that ship together as one
monolith. It does not cover cross-team coordination or per-module release
pipelines, and
none of its rules assume service mesh, API gateway, service discovery, saga,
circuit breaker, or distributed transaction semantics. Distributed-system
concerns belong to the [microservices governance framework](https://github.com/jesusarielgb-works/microservices-governance-framework).

## Ready when

- [ ] Every document above has its INSTRUCTIONS block removed, except `documentation-rules.md` and `_template-sprint-retro.md`, whose blocks explain how the file itself is used
- [ ] `definition-of-ready.md` and `definition-of-done.md` are both referenced from `01-context` onward
- [ ] The security policy names an owner, not just a document

---

**Related:** [`00-sdd-guide.md`](../00-sdd-guide.md) · [`documentation-rules.md`](./documentation-rules.md)
