# dashboard — Module Overview

## Purpose

Computes the KPIs and the recent-transactions list a business owner reads
first. It is a read-only aggregation: it is the authoritative source of
nothing, only of how those figures are computed.

## Public API

| Method | Input | Output | Called by |
|---|---|---|---|
| `DashboardFacade.GetDashboard` | — | totalIncome, totalExpenses, netBalance, recentTransactions[] (max 5) | `platform/web` (`GET /dashboard`) |

Everything not listed above is `internal/` and unreachable from outside this
module — see [`../../05-architecture/module-structure.md`](../../05-architecture/module-structure.md).

## Dependencies

| Depends on | Through | Why |
|---|---|---|
| `TransactionsFacade` | public API call | Read totals (`GetTotals`) and the 5 most recent transactions (`ListTransactions`); `dashboard` persists nothing of its own |

## In-process events

| Event | Direction | Other module(s) involved |
|---|---|---|
| — | — | None: `dashboard` only reads, on demand, inside the request that asks for it |

An in-process event is a synchronous call inside the same request, same
transaction, same release — never a queue or a broker.

---

**Related:** [`./data-model.md`](./data-model.md) · [`../module-catalog.md`](../module-catalog.md)
