# HU-11 — See the Dashboard Against the Budget, Globally or by Category

## Story

**As** a person who uses the application
**I want** to see the dashboard for the whole business or for one category, against its budget
**so that** I know how spending is going compared with what was planned

## Acceptance criteria

```gherkin
Scenario: Global budget progress
  Given the expense budgets in force today add up to 1,000.00 and 400.00 was spent in them
  When  I open the dashboard
  Then  I see budget 1,000.00, spent 400.00 and available 600.00

Scenario: Filter by category
  Given the category "Oficina" has a budget of 500.00 and 450.00 was spent
  When  I choose "Oficina" in the filter
  Then  the totals, the budget and the recent transactions are only of "Oficina"
  And   I see available 50.00

Scenario: Back to global
  Given the filter shows "Oficina"
  When  I choose "Todas"
  Then  I see the global figures again

Scenario: Over budget
  Given a budget of 500.00 and 600.00 spent
  When  I open the dashboard
  Then  available shows -100.00, marked as over budget

Scenario: No budget in force
  Given no budget is in force today
  When  I open the dashboard
  Then  I see "Sin presupuesto vigente"

Scenario: A user's totals are their own, the budget is the business's
  Given Ana, with the role USER, recorded 100.00 and Leo 300.00, in a budget of 1,000.00
  When  Ana opens the dashboard
  Then  her total expenses are 100.00
  And   she sees budget 1,000.00, spent 400.00 and available 600.00
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `dashboard` |
| Applicable NFR(s) | `NFR-01`, `NFR-02` |
| Business rule(s) touched | `BR-03`, `BR-04`, `BR-08` |
| Repositories | `bud-tack-docs`, `-api`, `-portal` |
| Contract | [`dashboard.yaml`](../../07-api/contracts/openapi/dashboard.yaml) — `GET /dashboard?categoryId=` |
| Screen | [`dashboard.md`](../../12-ux-ui/screens/dashboard.md) |
| Test case | `TC-025` |
| Blocked by | `HU-07`, `HU-08` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] `dashboard` reads only through `TransactionsFacade`; it owns no table and adds no migration

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
