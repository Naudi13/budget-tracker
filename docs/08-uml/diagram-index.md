# 08 — Diagram Index

> [!NOTE] INSTRUCTIONS
> One row per diagram that earns its keep — an outdated diagram is worse than
> no diagram. Add a row before adding a source file, and delete a row the
> same day its source file is deleted. Delete this block once the table
> matches `diagrams/source/` exactly.

## Diagrams

| Diagram | C4 level | Source file | Last updated |
|---|---|---|---|
| System context | Context | `diagrams/source/context.mmd` | 2026-09-12 |
| Single deployable | Container | `diagrams/source/container.mmd` | 2026-09-12 |
| `catalog` internals | Component | `diagrams/source/catalog-component.mmd` | 2026-09-12 |
| `billing` internals | Component | `diagrams/source/billing-component.mmd` | 2026-09-12 |

## What each level answers

| Level | Question | Boxes it draws |
|---|---|---|
| Context | Who and what surrounds the application? | The application, its users, external systems it calls |
| Container | What is the single deployable made of? | The one deployable and the one database — nothing else |
| Component | What lives inside one module? | A facade box, plus its internal domain/application/persistence groups |

## Where the model stops

This project has exactly one container, described in
[`../05-architecture/modular-monolith.md`](../05-architecture/modular-monolith.md).
C4 goes no deeper than component: a code-level diagram documents what the
`boundary check` already enforces, and goes stale on the next refactor.

---

**Related:** [`./README.md`](./README.md) · [`../09-modules/module-catalog.md`](../09-modules/module-catalog.md)
