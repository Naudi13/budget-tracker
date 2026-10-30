# TC-003 — Block Deleting a Category That Owns a Subcategory

## Identification

| Field | Value |
|---|---|
| Id | TC-003 |
| Covers | `HU-03` |
| Layer | `module-integration` |
| Module | `transactions` |

## Preconditions

- Expense category "Office" owns subcategory "Supplies", against a real
  database.

## Steps

1. Call `TransactionsFacade.DeleteCategoryAsync` for category "Office",
   from outside the `transactions` module.

## Expected result

The call fails per **BR-05**; the category row and its `is_active` value are
unchanged. A direct `DELETE` against `transactions.categories` for the same
row is separately refused by the `ON DELETE RESTRICT` key from
`subcategories.category_id`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
