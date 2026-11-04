# HU-07 — Set, Change or Delete a Budget for a Category

## Story

**As** a business owner
**I want** to set an amount limit for a category over a period, and change or delete it later
**so that** my written plan for that category stays the one I actually follow

## Acceptance criteria

```gherkin
Scenario: Set a budget for a category
  Given the active expense category "Office" exists
  When  I set a budget of 500.00 for "Office" from 2026-11-01 to 2026-11-30
  Then  the budget appears in the budget list for "Office"

Scenario: Reject a period that ends before it starts
  When  I set a budget from 2026-11-30 to 2026-11-01
  Then  it is not saved
  And   I see "The end date must be after the start date."

Scenario: Reject an amount of zero or less
  When  I set a budget of 0 for "Office"
  Then  it is not saved
  And   I see "Enter an amount above zero."

Scenario: Change a budget (BR-06)
  Given a budget of 500.00 for "Office" exists
  When  I change its amount to 650.00
  Then  the list shows 650.00 for "Office"

Scenario: Reject a change that breaks the period (BR-06)
  Given a budget for "Office" from 2026-11-01 to 2026-11-30 exists
  When  I change its end date to 2026-10-15
  Then  the budget keeps its previous dates
  And   I see "The end date must be after the start date."

Scenario: Delete a budget after confirming (BR-06)
  Given a budget for "Office" exists
  When  I delete it and confirm
  Then  it is no longer listed
  And   if "Office" has no subcategory, "Office" itself can now be deleted (BR-05)

Scenario: Inactive categories are not offered (BR-05)
  Given the category "Office" is inactive
  When  I open the budget form, to set or to change a budget
  Then  "Office" is not in the category list

Scenario: Filter budgets by category
  Given budgets exist for "Office" and for "Travel"
  When  I filter the list by "Office"
  Then  I only see the budgets for "Office"
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `transactions` |
| Applicable NFR(s) | `NFR-05`, `NFR-07` |
| Business rule(s) touched | `BR-05`, `BR-06` |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`budgets.yaml`](../../07-api/contracts/openapi/budgets.yaml) — `GET` and `POST /budgets`, `PATCH` and `DELETE /budgets/{budgetId}` |
| Screen | [`budgets.md`](../../12-ux-ui/screens/budgets.md) |
| Test cases | `TC-007`, `TC-019` |
| Blocked by | `HU-02` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] Comparing a budget against actual spending is out of scope in V1 — see [`scope.md`](../../01-context/scope.md)

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../01-context/scope.md`](../../01-context/scope.md)
