# TC-019 — Change and Delete a Budget, Keeping Its Invariants

## Identification

| Field | Value |
|---|---|
| Id | TC-019 |
| Covers | `HU-07` |
| Layer | `module-integration` |
| Module | `transactions` |

## Preconditions

- Expense category "Office" exists with no subcategory, against a real database.
- A budget for "Office" exists: `amount: 500`, `periodStart: 2026-11-01`,
  `periodEnd: 2026-11-30`.

## Steps

1. Call `TransactionsFacade.UpdateBudgetAsync` for that budget with
   `periodEnd: 2026-10-15`, from outside the `transactions` module.
2. Call `TransactionsFacade.UpdateBudgetAsync` for that budget with `amount: 650`.
3. Call `TransactionsFacade.DeleteCategoryAsync` for "Office".
4. Call `TransactionsFacade.DeleteBudgetAsync` for that budget.
5. Call `TransactionsFacade.DeleteCategoryAsync` for "Office" again.

## Expected result

1. Fails: the changed period would end before it starts (**BR-06**); the row
   keeps `period_end = 2026-11-30`. The API responds `422 Unprocessable Entity`.
2. Succeeds; the row now has `amount = 650`.
3. Fails per **BR-05**: the category still owns a budget.
4. Succeeds; no row for that budget remains in `transactions.budgets` (**BR-06**).
5. Succeeds: nothing else blocks the delete.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
