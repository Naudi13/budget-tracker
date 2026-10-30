# TC-002 — Reject a Subcategory Name Duplicated Within Its Category

## Identification

| Field | Value |
|---|---|
| Id | TC-002 |
| Covers | `HU-02` |
| Layer | `unit` |
| Module | `transactions` |

## Preconditions

- Expense category "Office" already owns a subcategory named "Supplies".

## Steps

1. Call `TransactionsFacade.CreateSubcategoryAsync` for category "Office"
   with name "Supplies".

## Expected result

The call fails: a subcategory's name must be unique within its category. No
row is inserted into `transactions.subcategories`; the API responds `409
Conflict`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../04-requirements/traceability-matrix.md`](../../04-requirements/traceability-matrix.md)
