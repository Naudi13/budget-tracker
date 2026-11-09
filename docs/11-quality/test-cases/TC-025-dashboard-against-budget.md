# TC-025 — The Dashboard Compares Spending with the Budget

## Identification

| Field | Value |
|---|---|
| Id | TC-025 |
| Covers | `HU-11` |
| Layer | `end-to-end` |
| Module | `dashboard` |

## Preconditions

- A budget of 500.00 in force for `Oficina`. `USER` A recorded 100.00 and `USER` B 300.00 in it.

## Steps

1. `GET /dashboard` as A.
2. `GET /dashboard` as `ADMIN`.
3. `GET /dashboard?categoryId=` of `Oficina` as A.
4. `GET /dashboard` with no budget in force.
5. `GET /dashboard?categoryId=` of an unknown id.

## Expected result

1. Total expenses 100.00; budget 500.00, spent 400.00, available 100.00.
2. Total expenses 400.00; the same budget figures.
3. Everything only of `Oficina`.
4. `budget` is null.
5. `404`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md)
