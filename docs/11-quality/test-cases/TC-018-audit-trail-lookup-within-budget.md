# TC-018 — Find a Soft-Deleted Transaction's Audit Trail Within Five Minutes

## Identification

| Field | Value |
|---|---|
| Id | TC-018 |
| Covers | `NFR-10` |
| Layer | other — quarterly support drill, outside the three code layers |
| Module | `transactions` |

## Preconditions

- A transaction has already been soft-deleted in `production`.

## Steps

1. Start a timer.
2. Using only logs and the audit trail, locate the transaction's
   `deleted_by` and `deleted_at`.
3. Stop the timer.

## Expected result

Elapsed time is under 5 minutes.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../13-operations/observability.md`](../../13-operations/observability.md)
