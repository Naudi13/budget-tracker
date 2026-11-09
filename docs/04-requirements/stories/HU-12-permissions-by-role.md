# HU-12 — Permissions by Role

## Story

**As** an administrator
**I want** only administrators to change categories, subcategories and budgets
**so that** the people recording transactions work on one organized catalog

## Acceptance criteria

```gherkin
Scenario: An administrator manages the catalog
  Given I am signed in with the role ADMIN
  Then  I can create, read, change and delete categories, subcategories and budgets

Scenario: A user cannot change the catalog
  Given I am signed in with the role USER
  When  I create, change or delete a category, subcategory or budget
  Then  the API answers 403 and nothing changes

Scenario: A user cannot read the budgets endpoint
  Given I am signed in with the role USER
  When  I call GET /budgets
  Then  the API answers 403

Scenario: A user picks from the catalog
  Given I am signed in with the role USER
  When  I record a transaction or filter the dashboard
  Then  the active categories created by an administrator are offered

Scenario: A user only has their screens
  Given I am signed in with the role USER
  Then  the menu offers only Panel and Movimientos
  And   opening /settings/categories, /settings/budgets or /settings/users goes to the dashboard
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `transactions` |
| Applicable NFR(s) | `NFR-05` |
| Business rule(s) touched | none |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`categories.yaml`](../../07-api/contracts/openapi/categories.yaml) · [`budgets.yaml`](../../07-api/contracts/openapi/budgets.yaml) |
| Screens | [`categories-configuration.md`](../../12-ux-ui/screens/categories-configuration.md) · [`budgets.md`](../../12-ux-ui/screens/budgets.md) |
| Test case | `TC-024` |
| Blocked by | `HU-02`, `HU-07` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`ADR-009`](../../05-architecture/decisions/records/ADR-009-one-business-with-a-shared-catalog.md)
