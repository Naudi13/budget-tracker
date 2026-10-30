# TC-007 — Reject a Budget Whose Period End Is Not After Its Start

## Identification

| Field | Value |
|---|---|
| Id | TC-007 |
| Covers | `HU-07` |
| Layer | `unit` |
| Module | `transactions` |

## Preconditions

- Expense category "Office" exists.

## Steps

1. Call `TransactionsFacade.CreateBudgetAsync` for category "Office" with
   `amount: 500`, `periodStart: 2026-11-01`, `periodEnd: 2026-10-31`.

## Expected result

The call fails: a budget's `period_end` must be after its `period_start`. No
row is inserted into `transactions.budgets`; the API responds `422
Unprocessable Entity`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
