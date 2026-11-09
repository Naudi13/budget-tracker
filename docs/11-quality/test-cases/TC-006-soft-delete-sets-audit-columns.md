# TC-006 — Soft-Deleting a Transaction Sets `deleted_by` and `deleted_at`

## Identification

| Field | Value |
|---|---|
| Id | TC-006 |
| Covers | `HU-06` |
| Layer | `unit` |
| Module | `transactions` |

## Preconditions

- Transaction T1 exists, not deleted. The caller is user U1.

## Steps

1. Call `TransactionsFacade.DeleteAsync(T1.Id)` as U1, with the reason "Monto equivocado".
2. Read T1's row directly through persistence, bypassing the facade.

## Expected result

T1's `deleted_by` equals U1's id, `deleted_at` is set to the current time and
`deletion_reason` holds the reason;
the row still exists in `transactions.transactions` — it is never physically
removed, per **BR-04**.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../06-data/database-conventions.md`](../../06-data/database-conventions.md)
