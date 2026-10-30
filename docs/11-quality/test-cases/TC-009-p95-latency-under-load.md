# TC-009 — p95 Latency Under Load on Read Endpoints

## Identification

| Field | Value |
|---|---|
| Id | TC-009 |
| Covers | `NFR-01` |
| Layer | other — scripted load test, outside the three code layers |
| Module | whole artifact |

## Preconditions

- `staging` runs the latest artifact, seeded with a representative dataset.

## Steps

1. Run the scripted load test against `GET /api/v1/transactions` and
   `GET /api/v1/dashboard` at 50 concurrent users, before release.

## Expected result

p95 response time is under 300 ms on both endpoints; the run's report is
attached to the release.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../04-requirements/non-functional.md`](../../04-requirements/non-functional.md)
