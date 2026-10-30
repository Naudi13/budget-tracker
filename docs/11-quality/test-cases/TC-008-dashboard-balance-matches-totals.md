# TC-008 — Dashboard Balance Equals Income Minus Expense Totals

## Identification

| Field | Value |
|---|---|
| Id | TC-008 |
| Covers | `HU-08` |
| Layer | `end-to-end` |
| Module | `dashboard` |

## Preconditions

- Seeded, active transactions for one business: one income detail of
  `1000.00` and one expense detail of `350.00`.

## Steps

1. Send `GET /api/v1/dashboard` with a valid bearer token.

## Expected result

`200 OK`; `totalIncome` is `1000.00`, `totalExpenses` is `350.00`, and
`netBalance` is `650.00`, computed on read per **BR-03**.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../07-api/contracts/openapi/dashboard.yaml`](../../07-api/contracts/openapi/dashboard.yaml)
