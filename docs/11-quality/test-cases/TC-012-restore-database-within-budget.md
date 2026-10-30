# TC-012 — Restore the Database from the Latest Backup Within 30 Minutes

## Identification

| Field | Value |
|---|---|
| Id | TC-012 |
| Covers | `NFR-04` |
| Layer | other — quarterly restore drill, outside the three code layers |
| Module | whole artifact |

## Preconditions

- A current backup exists in the deployment platform's backup store.

## Steps

1. Start a timer.
2. Restore the latest backup to a fresh instance.
3. Stop the timer once the restored instance accepts a connection and a
   smoke query succeeds.

## Expected result

Elapsed time is under 30 minutes. This test case cannot be executed yet: no
restore procedure is documented anywhere in this repository — see
[`traceability-matrix.md`](../../04-requirements/traceability-matrix.md)'s Gaps.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../04-requirements/non-functional.md`](../../04-requirements/non-functional.md)
