# HU-03 — Delete or Deactivate a Category or Subcategory

## Story

**As** a business owner
**I want** to delete a category or subcategory I no longer need, or deactivate it when it is already in use
**so that** my lists stay short without losing the history of past transactions

## Acceptance criteria

```gherkin
Scenario: Delete an unused subcategory
  Given the subcategory "Supplies" is referenced by no transaction detail
  When  I delete "Supplies" and confirm
  Then  it no longer appears under its category

Scenario: Block deleting a subcategory in use (BR-05)
  Given the subcategory "Supplies" is referenced by a transaction detail
  When  I delete "Supplies"
  Then  it is not deleted
  And   I see "'Supplies' is used by recorded transactions. Deactivate it instead."

Scenario: Block deleting a category that owns a subcategory or a budget (BR-05)
  Given the category "Office" owns the subcategory "Supplies"
  When  I delete "Office"
  Then  it is not deleted
  And   I am offered to deactivate it instead

Scenario: Deactivate a category in use
  Given the category "Office" owns a subcategory used by a transaction
  When  I deactivate "Office"
  Then  "Office" and its subcategories are no longer offered for new transactions or budgets
  And   existing transactions still show "Office"

Scenario: Reactivate a deactivated subcategory
  Given the subcategory "Supplies" is inactive
  When  I activate "Supplies"
  Then  it is offered again when I record an expense
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `transactions` |
| Applicable NFR(s) | `NFR-05`, `NFR-07` |
| Business rule(s) touched | `BR-05` |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`categories.yaml`](../../07-api/contracts/openapi/categories.yaml) — `DELETE` and `PATCH` on `/categories/{categoryId}` and its subcategories |
| Screen | [`categories-configuration.md`](../../12-ux-ui/screens/categories-configuration.md) |
| Test case | `TC-003` |
| Blocked by | `HU-02`; the "in use by a transaction" scenarios also need `HU-04`, and "owns a budget" needs `HU-07` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] Deactivating a category leaves its subcategories' own `is_active` untouched; they are hidden because their category is inactive

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
