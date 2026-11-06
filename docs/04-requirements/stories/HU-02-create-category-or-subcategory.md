# HU-02 — Create a Category or Subcategory

## Story

**As** a business owner
**I want** to create my own income and expense categories, each with its subcategories
**so that** every transaction I record is classified in my business's own words

## Acceptance criteria

```gherkin
Scenario: Create a category
  Given I am signed in
  And   no expense category is named "Office"
  When  I create an expense category named "Office"
  Then  "Office" appears in the expense category list as active

Scenario: Create a subcategory inside a category
  Given the expense category "Office" exists
  When  I create a subcategory named "Supplies" in "Office"
  Then  "Supplies" appears under "Office" as active
  And   it is offered when I record an expense

Scenario: Reject a category name duplicated within its type
  Given an expense category named "Office" exists
  When  I create another expense category named "Office"
  Then  it is not created
  And   I see "Ya existe una categoría de gasto llamada 'Office'."

Scenario: Allow the same name in the other type
  Given an expense category named "Other" exists
  When  I create an income category named "Other"
  Then  it is created

Scenario: Reject a subcategory name duplicated within its category
  Given the category "Office" owns a subcategory named "Supplies"
  When  I create another subcategory named "Supplies" in "Office"
  Then  it is not created
  And   I see "Esta categoría ya tiene una subcategoría llamada 'Supplies'."
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `transactions` |
| Applicable NFR(s) | `NFR-05`, `NFR-07` |
| Business rule(s) touched | none — the uniqueness invariants in [`entities-and-rules.md`](../../02-domain/entities-and-rules.md) |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`categories.yaml`](../../07-api/contracts/openapi/categories.yaml) — `POST /categories`, `POST /categories/{categoryId}/subcategories` |
| Screen | [`categories-configuration.md`](../../12-ux-ui/screens/categories-configuration.md) |
| Test case | `TC-002` |
| Blocked by | `HU-01` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
