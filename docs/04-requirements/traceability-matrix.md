# 04 — Traceability Matrix

## How to read these tables

`Requirement -> Module -> Design document -> Test case`. Both tables read that
way: the first closes every `HU-NN`, the second every `NFR-NN`. A cell may point
at a section filled in later than this one — that forward link is expected, not
a defect.

## Stories

| HU-NN | Module | Design document | Test case |
|---|---|---|---|
| HU-01 | `identity` | [`identity.yaml`](../07-api/contracts/openapi/identity.yaml) · [`sign-in.md`](../12-ux-ui/screens/sign-in.md) · [`sign-up.md`](../12-ux-ui/screens/sign-up.md) | [`TC-001`](../11-quality/test-cases/TC-001-reject-duplicate-signup-email.md) |
| HU-02 | `transactions` | [`categories.yaml`](../07-api/contracts/openapi/categories.yaml) · [`categories-configuration.md`](../12-ux-ui/screens/categories-configuration.md) | [`TC-002`](../11-quality/test-cases/TC-002-reject-duplicate-subcategory-name.md) |
| HU-03 | `transactions` | [`categories.yaml`](../07-api/contracts/openapi/categories.yaml) · [`categories-configuration.md`](../12-ux-ui/screens/categories-configuration.md) | [`TC-003`](../11-quality/test-cases/TC-003-block-delete-category-with-subcategory.md) |
| HU-04 | `transactions` | [`transactions.yaml`](../07-api/contracts/openapi/transactions.yaml) · [`new-record.md`](../12-ux-ui/screens/new-record.md) | [`TC-004`](../11-quality/test-cases/TC-004-reject-mixed-type-transaction.md) |
| HU-05 | `transactions` | [`transactions.yaml`](../07-api/contracts/openapi/transactions.yaml) · [`transaction-history.md`](../12-ux-ui/screens/transaction-history.md) | [`TC-005`](../11-quality/test-cases/TC-005-exclude-soft-deleted-from-history.md) |
| HU-06 | `transactions` | [`transactions.yaml`](../07-api/contracts/openapi/transactions.yaml) · [`transaction-history.md`](../12-ux-ui/screens/transaction-history.md) | [`TC-006`](../11-quality/test-cases/TC-006-soft-delete-sets-audit-columns.md) |
| HU-07 | `transactions` | [`budgets.yaml`](../07-api/contracts/openapi/budgets.yaml) · [`budgets.md`](../12-ux-ui/screens/budgets.md) | [`TC-007`](../11-quality/test-cases/TC-007-reject-invalid-budget-period.md) · [`TC-019`](../11-quality/test-cases/TC-019-change-and-delete-budget.md) |
| HU-08 | `dashboard` | [`dashboard.yaml`](../07-api/contracts/openapi/dashboard.yaml) · [`dashboard.md`](../12-ux-ui/screens/dashboard.md) | [`TC-008`](../11-quality/test-cases/TC-008-dashboard-balance-matches-totals.md) |
| HU-09 | `identity` | [`identity.yaml`](../07-api/contracts/openapi/identity.yaml) · [`users.md`](../12-ux-ui/screens/users.md) | [`TC-020`](../11-quality/test-cases/TC-020-only-admin-lists-accounts.md) |

Each Design document cell names the API contract that defines the story's
request and response shape, and the screen that serves it where one exists.
Once `identity/`, `transactions/`, and `dashboard/` exist under `09-modules/`,
each story can add its own module folder alongside these.

## Non-functional requirements

| NFR-NN | Module | Design document | Test case |
|---|---|---|---|
| NFR-01 | whole artifact | [`observability.md`](../13-operations/observability.md) — the request-duration histogram | [`TC-009`](../11-quality/test-cases/TC-009-p95-latency-under-load.md) |
| NFR-02 | `dashboard` | [`models.md`](../06-data/models.md) — `transactions` and `transaction_details` | [`TC-010`](../11-quality/test-cases/TC-010-dashboard-renders-within-budget.md) |
| NFR-03 | whole artifact | [`observability.md`](../13-operations/observability.md) — `GET /health` | [`TC-011`](../11-quality/test-cases/TC-011-uptime-monitor-reports.md) |
| NFR-04 | whole artifact | none yet — see Gaps | [`TC-012`](../11-quality/test-cases/TC-012-restore-database-within-budget.md) — cannot run yet, see Gaps |
| NFR-05 | `identity`, `transactions` | [`rest-conventions.md`](../07-api/rest-conventions.md) — the 401/403 rules | [`TC-013`](../11-quality/test-cases/TC-013-reject-unauthenticated-writes.md) |
| NFR-06 | `identity` | [`security-policy.md`](../00-governance/security-policy.md) — password storage | [`TC-014`](../11-quality/test-cases/TC-014-password-stored-as-hash.md) |
| NFR-07 | whole artifact | [`definition-of-done.md`](../00-governance/definition-of-done.md) — the coverage threshold | [`TC-015`](../11-quality/test-cases/TC-015-ci-blocks-below-coverage.md) |
| NFR-08 | whole artifact | [`local-setup.md`](../10-devops/local-setup.md) — the prerequisites and steps | [`TC-016`](../11-quality/test-cases/TC-016-onboarding-within-one-hour.md) |
| NFR-09 | whole artifact | [`observability.md`](../13-operations/observability.md) — the structured log fields | [`TC-017`](../11-quality/test-cases/TC-017-unhandled-error-logged.md) |
| NFR-10 | `transactions` | [`observability.md`](../13-operations/observability.md) — the audit trail lookup | [`TC-018`](../11-quality/test-cases/TC-018-audit-trail-lookup-within-budget.md) |

Every row above is proved by a gate, a monitor, a timed run or a scheduled drill
rather than by a story scenario, and some of them run outside the three layers in
[`testing-strategy.md`](../11-quality/testing-strategy.md), which says exactly
that of **NFR-01**. Each `TC-NNN`'s Layer field says so, instead of naming one
of the three code layers.

## Gaps

A gap is an `HU-NN` or `NFR-NN` with no row above, or a row whose design
document or test case does not exist. One is open:

- **NFR-04** sets a 30-minute restore target that no document in this
  repository describes a procedure for; it is written once `bud-tack-infra`
  defines the staging and production databases.

Each story's acceptance criteria are in its own file under
[`stories/`](./stories/HU-01-sign-in.md), linked from
[`user-stories.md`](./user-stories.md).

Every row above now carries a real `TC-NNN` from
[`11-quality/test-cases/`](../11-quality/_template-test-case.md). `TC-012` is
the one exception: it exists, but cannot be executed until NFR-04's gap above
is closed.

---

**Related:** [`./user-stories.md`](./user-stories.md) · [`./non-functional.md`](./non-functional.md)
