# HU-05 — View Transaction History

## Story

**As** a business owner or a member of the small business staff
**I want** to see every recorded transaction, newest first
**so that** I can check what was recorded and spot a mistake

## Acceptance criteria

```gherkin
Scenario: List transactions newest first
  Given transactions dated 2026-10-01, 2026-10-15 and 2026-10-20 exist
  When  I open the transaction history
  Then  I see them in the order 2026-10-20, 2026-10-15, 2026-10-01
  And   each row shows its date, description, type and total amount

Scenario: Paginate a long history
  Given 45 transactions exist
  When  I open the transaction history
  Then  I see the 20 newest on the first page
  And   I can move to pages 2 and 3

Scenario: Exclude deleted transactions (BR-04)
  Given the transaction "Printer paper" was deleted
  When  I open the transaction history
  Then  "Printer paper" is not listed

Scenario: Empty history
  Given no transaction has been recorded
  When  I open the transaction history
  Then  I see "Aún no has registrado movimientos" and a button to record the first one
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `transactions` |
| Applicable NFR(s) | `NFR-01`, `NFR-05` |
| Business rule(s) touched | `BR-04` |
| Repositories | `bud-tack-docs`, `-api`, `-portal` |
| Contract | [`transactions.yaml`](../../07-api/contracts/openapi/transactions.yaml) — `GET /transactions?page=&limit=` |
| Screen | [`transaction-history.md`](../../12-ux-ui/screens/transaction-history.md) |
| Test case | `TC-005` |
| Blocked by | `HU-04` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] Transactions with the same date are ordered by creation time, newest first

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../07-api/rest-conventions.md`](../../07-api/rest-conventions.md)
