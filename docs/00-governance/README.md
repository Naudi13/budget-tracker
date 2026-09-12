# 00 — Governance

> [!NOTE] INSTRUCTIONS
> This README is the entry point for the section: it lists every document, states
> what "done" means for the section as a whole, and declares what the section does
> not cover. Every other section's `README.md` copies this shape. Delete this
> block once a second reviewer has read the section.

## Purpose

This section holds the rules the whole project commits to before any other
section is filled in: how work enters a sprint, how it is closed, how branches
and commits are named, which ceremonies run, how documentation is written, and
how secrets and dependencies are kept safe inside a single deployable
application.

## Documents in this section

| Document | Answers |
|---|---|
| [definition-of-ready.md](./definition-of-ready.md) | When can a story enter a sprint? |
| [definition-of-done.md](./definition-of-done.md) | When is a story finished? |
| [git-conventions.md](./git-conventions.md) | How are branches and commits named? |
| [agile-conventions.md](./agile-conventions.md) | What ceremonies run, and what do they produce? |
| [documentation-rules.md](./documentation-rules.md) | When is a document done? |
| [security-policy.md](./security-policy.md) | How are secrets, dependencies and access protected? |
| [_template-sprint-retro.md](./_template-sprint-retro.md) | Template — copy it once per sprint |

## When this section is done

- [ ] Every document above has its INSTRUCTIONS block removed, except `documentation-rules.md`
- [ ] A new contributor can name a branch, open a pull request and pass review without asking a question
- [ ] `definition-of-ready.md` and `definition-of-done.md` are both referenced from `01-context` onward
- [ ] The security policy names an owner, not just a document

## Out of scope

This section governs one deployable application built and released by one team.
It does not cover cross-team coordination, per-module release pipelines, or any
multi-service pattern — service mesh, API gateway, service discovery, saga,
circuit breaker, distributed transaction. A project that splits into
independently deployed services should govern itself with the sibling
[microservices-governance-framework](https://github.com/jesusarielgb-works/microservices-governance-framework)
instead of adapting this one.

---

**Related:** [`00-sdd-guide.md`](../00-sdd-guide.md) · [`documentation-rules.md`](./documentation-rules.md)
