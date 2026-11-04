# HU-04 — Register an Income or Expense Transaction

## Story

**As** a business owner or a member of the small business staff
**I want** to record a transaction with its date, description and one or more detail lines
**so that** the balance always reflects the money that actually moved

## Acceptance criteria

```gherkin
Scenario: Record an expense with one detail line
  Given the active expense subcategory "Office > Supplies" exists
  When  I record a transaction dated today, described "Printer paper",
        with one line of 85.50 against "Supplies"
  Then  the transaction is saved and I return to the dashboard
  And   total expenses grow by 85.50

Scenario: Record a transaction with several detail lines of one type
  Given the active income subcategories "Sales > Store" and "Sales > Online" exist
  When  I record one transaction with 200.00 against "Store" and 150.00 against "Online"
  Then  it is saved as an income transaction of 350.00 in total

Scenario: The type comes from the subcategory, never from the client (BR-01)
  Given the expense subcategory "Supplies" exists
  When  I record a line against "Supplies"
  Then  that line is stored and shown as an expense

Scenario: Reject a transaction mixing income and expense (BR-02)
  Given a transaction being recorded has a line against an income subcategory
  When  I add a line against an expense subcategory and save
  Then  nothing is saved
  And   I see "A transaction cannot mix income and expense lines."

Scenario: Reject an amount of zero or less
  When  I record a line with amount 0
  Then  nothing is saved
  And   I see "Enter an amount above zero."

Scenario: Inactive subcategories are not offered (BR-05)
  Given the subcategory "Supplies" is inactive
  When  I open the new record form
  Then  "Supplies" is not in the subcategory list
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `transactions` |
| Applicable NFR(s) | `NFR-01`, `NFR-05`, `NFR-07` |
| Business rule(s) touched | `BR-01`, `BR-02`, `BR-05` |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`transactions.yaml`](../../07-api/contracts/openapi/transactions.yaml) — `POST /transactions` |
| Screen | [`new-record.md`](../../12-ux-ui/screens/new-record.md) |
| Test case | `TC-004` |
| Blocked by | `HU-02` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] The transaction and all of its lines are written in one database transaction: all or nothing

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
