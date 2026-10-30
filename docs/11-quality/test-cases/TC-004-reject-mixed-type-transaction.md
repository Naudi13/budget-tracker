# TC-004 — Reject a Transaction Mixing Income and Expense Details

## Identification

| Field | Value |
|---|---|
| Id | TC-004 |
| Covers | `HU-04` |
| Layer | `module-integration` |
| Module | `transactions` |

## Preconditions

- Active income subcategory "Sales" and active expense subcategory "Rent"
  both exist, against a real database.

## Steps

1. Call `TransactionsFacade.RecordAsync` with two details: one on "Sales"
   and one on "Rent".

## Expected result

The call fails per **BR-02**; no row is inserted into
`transactions.transactions` or `transactions.transaction_details`. The API
responds `422 Unprocessable Entity` with error code `BUSINESS_RULE_VIOLATED`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
