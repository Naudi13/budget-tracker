# TC-016 — A New Developer Reaches a First Successful Request Within One Hour

## Identification

| Field | Value |
|---|---|
| Id | TC-016 |
| Covers | `NFR-08` |
| Layer | other — timed onboarding run, outside the three code layers |
| Module | whole artifact |

## Preconditions

- A machine with none of the prerequisites installed.

## Steps

1. Follow [`local-setup.md`](../../10-devops/local-setup.md) from clone
   through its verification step, timed from the start.

## Expected result

`GET /health` returns `200` before 60 minutes have elapsed, signed off by
the tech lead.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../10-devops/local-setup.md`](../../10-devops/local-setup.md)
