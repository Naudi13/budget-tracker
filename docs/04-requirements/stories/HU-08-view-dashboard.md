# HU-08 — View the Dashboard

## Story

**As** a business owner
**I want** one screen with my total income, total expenses, net balance and the latest transactions
**so that** I know my financial position at a glance, without adding anything up myself

## Acceptance criteria

```gherkin
Scenario: Show the KPIs
  Given income details totalling 1,000.00 and expense details totalling 400.00 exist
  When  I open the dashboard
  Then  I see total income 1,000.00, total expenses 400.00 and net balance 600.00

Scenario: The balance is computed on read (BR-03)
  Given the dashboard shows a net balance of 600.00
  When  I record an expense of 100.00 and return to the dashboard
  Then  the net balance shows 500.00 with no manual refresh

Scenario: Deleted transactions do not count (BR-04)
  Given an expense of 100.00 was deleted
  When  I open the dashboard
  Then  it is not in the totals nor in the recent list

Scenario: Show the five most recent transactions
  Given 8 transactions exist
  When  I open the dashboard
  Then  I see the 5 newest, newest first, and a link to the full history

Scenario: Empty dashboard
  Given no transaction has been recorded
  When  I open the dashboard
  Then  the three KPIs show 0.00
  And   the recent list says "Aún no tienes movimientos" with a button to record one
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `dashboard` |
| Applicable NFR(s) | `NFR-01`, `NFR-02` |
| Business rule(s) touched | `BR-03`, `BR-04` |
| Repositories | `bud-tack-docs`, `-api`, `-portal` |
| Contract | [`dashboard.yaml`](../../07-api/contracts/openapi/dashboard.yaml) — `GET /dashboard` |
| Screen | [`dashboard.md`](../../12-ux-ui/screens/dashboard.md) |
| Test cases | `TC-008`, `TC-010` |
| Blocked by | `HU-04`; the deleted-transaction scenario also needs `HU-06` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] `dashboard` reads only through `TransactionsFacade`; it owns no table and adds no migration

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/module-boundaries.md`](../../02-domain/module-boundaries.md)
