# TC-010 — Dashboard Renders Within Budget at 10,000 Transactions

## Identification

| Field | Value |
|---|---|
| Id | TC-010 |
| Covers | `NFR-02` |
| Layer | other — timed run, outside the three code layers |
| Module | `dashboard` |

## Preconditions

- A seeded staging dataset with 10,000 transactions for one business.

## Steps

1. Time a `GET /api/v1/dashboard` request from send to full response
   received.

## Expected result

Elapsed time is under 2 seconds.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../06-data/models.md`](../../06-data/models.md)
