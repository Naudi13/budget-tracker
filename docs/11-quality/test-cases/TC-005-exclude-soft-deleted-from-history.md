# TC-005 — Exclude a Soft-Deleted Transaction from History

## Identification

| Field | Value |
|---|---|
| Id | TC-005 |
| Covers | `HU-05` |
| Layer | `unit` |
| Module | `transactions` |

## Preconditions

- Transaction T1 exists with `deleted_at` already set.

## Steps

1. Call `TransactionsFacade.ListAsync` with `page: 1, limit: 20`.

## Expected result

T1 is not present in the returned page, and `PaginationMeta.total` does not
count it, per **BR-04**.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
