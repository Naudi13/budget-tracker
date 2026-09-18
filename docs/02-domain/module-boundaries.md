# 02 — Module Boundaries

## What a module is

A **module** is a bounded context that lives inside the single deployable. It owns
its data, exposes a narrow **public API**, and keeps everything else **internal**.

| Term | Meaning | Enforced by |
|---|---|---|
| Module | A bounded context with its own folder and its own tables | Folder layout |
| Public API | The only entry point other modules may call | Boundary linter (see `05-architecture/boundary-enforcement.md`) |
| Internal | Everything else in the module | Boundary linter |

## How to draw the boundary

1. Group behaviour by the **language the business uses**, not by technical layer.
   `Transactions` and `Dashboard` are modules; `Controllers` and `Repositories` are not.
2. A module should own the data it writes. If two modules write the same table,
   the boundary is in the wrong place.
3. Prefer **fewer, larger** modules at the start. Splitting is cheap; merging is not.

## Module map

| Module | Owns | Public API | Depends on |
|---|---|---|---|
| `transactions` | transactions, categories | `TransactionsFacade` | — |
| `dashboard` | — (no owned tables; reads and aggregates on demand) | `DashboardFacade` | `transactions` |

## Signals the boundary is wrong

| Signal | What it means |
|---|---|
| Every feature touches three modules | The boundary cuts across a real workflow |
| Two modules write the same table | Ownership is not settled |
| A module's public API has 20 methods | It is a layer, not a module |
| Circular dependency between modules | One of them should not exist |

---

**Related:** [`../05-architecture/modular-monolith.md`](../05-architecture/modular-monolith.md) · [`../09-modules/module-catalog.md`](../09-modules/module-catalog.md)
