# 08 — Diagram Index

## Diagrams

| Diagram | C4 level | Where it renders | Kept current by |
|---|---|---|---|
| System context | Context | Inline Mermaid, in [`../01-context/overview.md`](../01-context/overview.md) | The pull request that adds or drops an audience or an external system |
| Single deployable | Container | Inline Mermaid, in [`../05-architecture/modular-monolith.md`](../05-architecture/modular-monolith.md) | The pull request that adds or drops a module |
| `identity` internals | Component | Inline Mermaid, in [`../09-modules/identity/README.md`](../09-modules/identity/README.md) | The pull request that moves code across `identity`'s internal folders |
| `transactions` internals | Component | Inline Mermaid, in [`../09-modules/transactions/README.md`](../09-modules/transactions/README.md) | The pull request that moves code across `transactions`'s internal folders |
| `dashboard` internals | Component | Inline Mermaid, in [`../09-modules/dashboard/README.md`](../09-modules/dashboard/README.md) | The pull request that moves code across `dashboard`'s internal folders |

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

Nothing under `diagrams/` is used yet: every diagram above is Mermaid, which
GitHub renders inline. `diagrams/source/` and `diagrams/exports/` stay empty
until a diagram Mermaid cannot express is needed.

---

**Related:** [`./README.md`](./README.md) · [`../09-modules/module-catalog.md`](../09-modules/module-catalog.md)
