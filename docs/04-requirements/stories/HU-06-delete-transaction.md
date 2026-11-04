# HU-06 — Delete a Transaction

## Story

**As** a business owner
**I want** to delete a transaction I recorded by mistake
**so that** my totals are right again, while the record stays recoverable

## Acceptance criteria

```gherkin
Scenario: Delete a transaction after confirming
  Given the expense transaction "Printer paper" of 85.50 exists
  When  I delete it from the history and confirm
  Then  it disappears from the history and from the dashboard's recent list
  And   total expenses drop by 85.50

Scenario: Cancel a delete
  Given the transaction "Printer paper" exists
  When  I choose delete and then cancel
  Then  it is still listed and nothing changed

Scenario: A deleted transaction is kept with its audit trail (BR-04)
  Given I deleted the transaction "Printer paper"
  When  support looks it up in the audit trail
  Then  the row still exists with `deleted_by` set to my user and `deleted_at` set to the time of the delete

Scenario: A deleted transaction cannot be deleted again
  Given the transaction "Printer paper" was already deleted
  When  a request deletes it again
  Then  the request is answered with 404
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `transactions` |
| Applicable NFR(s) | `NFR-05`, `NFR-07`, `NFR-10` |
| Business rule(s) touched | `BR-04` |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`transactions.yaml`](../../07-api/contracts/openapi/transactions.yaml) — `DELETE /transactions/{transactionId}` |
| Screen | [`transaction-history.md`](../../12-ux-ui/screens/transaction-history.md) |
| Test cases | `TC-006`, `TC-018` |
| Blocked by | `HU-05` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] Restoring a deleted transaction is not part of this story; V1 only keeps the row recoverable by support

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
