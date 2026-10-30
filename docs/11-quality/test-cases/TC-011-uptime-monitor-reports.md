# TC-011 — `/health` Is Polled and Reports Monthly Uptime

## Identification

| Field | Value |
|---|---|
| Id | TC-011 |
| Covers | `NFR-03` |
| Layer | other — uptime monitor, outside the three code layers |
| Module | whole artifact |

## Preconditions

- The uptime monitor polls `GET /health` against `production` every 5
  minutes.

## Steps

1. Review the monitor's report for one calendar month.

## Expected result

Logged uptime is at least 99.5% for the month; any outage is recorded with
its start and end time.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../13-operations/observability.md`](../../13-operations/observability.md)
