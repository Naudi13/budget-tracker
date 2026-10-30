# 04 — Traceability Matrix

## How to read these tables

`Requirement -> Module -> Design document -> Test case`. Both tables read that
way: the first closes every `HU-NN`, the second every `NFR-NN`. A cell may point
at a section filled in later than this one — that forward link is expected, not
a defect.

## Stories

| HU-NN | Module | Design document | Test case |
|---|---|---|---|
| HU-01 | `identity` | [`identity.yaml`](../07-api/contracts/openapi/identity.yaml) — no screen yet, see Gaps | [`unit`](../11-quality/testing-strategy.md) — reject sign-up with an email already in use |
| HU-02 | `transactions` | [`categories.yaml`](../07-api/contracts/openapi/categories.yaml) · [`categories-configuration.md`](../12-ux-ui/screens/categories-configuration.md) | [`unit`](../11-quality/testing-strategy.md) — reject a subcategory name duplicated within its category |
| HU-03 | `transactions` | [`categories.yaml`](../07-api/contracts/openapi/categories.yaml) · [`categories-configuration.md`](../12-ux-ui/screens/categories-configuration.md) | [`module-integration`](../11-quality/testing-strategy.md) — a category or subcategory still referenced cannot be deleted (BR-05) |
| HU-04 | `transactions` | [`transactions.yaml`](../07-api/contracts/openapi/transactions.yaml) · [`new-record.md`](../12-ux-ui/screens/new-record.md) | [`module-integration`](../11-quality/testing-strategy.md) — a transaction cannot mix income and expense details (BR-02) |
| HU-05 | `transactions` | [`transactions.yaml`](../07-api/contracts/openapi/transactions.yaml) · [`transaction-history.md`](../12-ux-ui/screens/transaction-history.md) | [`unit`](../11-quality/testing-strategy.md) — a soft-deleted transaction is excluded from the history list (BR-04) |
| HU-06 | `transactions` | [`transactions.yaml`](../07-api/contracts/openapi/transactions.yaml) · [`transaction-history.md`](../12-ux-ui/screens/transaction-history.md) | [`unit`](../11-quality/testing-strategy.md) — deleting a transaction sets `deleted_by`/`deleted_at` instead of removing the row (BR-04) |
| HU-07 | `transactions` | [`budgets.yaml`](../07-api/contracts/openapi/budgets.yaml) — no screen yet, see Gaps | [`unit`](../11-quality/testing-strategy.md) — reject a budget whose `period_end` is not after `period_start` |
| HU-08 | `dashboard` | [`dashboard.yaml`](../07-api/contracts/openapi/dashboard.yaml) · [`dashboard.md`](../12-ux-ui/screens/dashboard.md) | [`end-to-end`](../11-quality/testing-strategy.md) — the balance shown equals total income details minus total expense details (BR-03) |

Each Design document cell names the API contract that defines the story's
request and response shape, and the screen that serves it where one exists.
Once `identity/`, `transactions/`, and `dashboard/` exist under `09-modules/`,
each story can add its own module folder alongside these.

## Non-functional requirements

| NFR-NN | Module | Design document | Test case |
|---|---|---|---|
| NFR-01 | whole artifact | [`observability.md`](../13-operations/observability.md) — the request-duration histogram | Scripted load test against staging, before each release |
| NFR-02 | `dashboard` | [`models.md`](../06-data/models.md) — `transactions` and `transaction_details` | Timed run against a seeded staging dataset |
| NFR-03 | whole artifact | [`observability.md`](../13-operations/observability.md) — `GET /health` | Uptime monitor polling `/health` every 5 minutes |
| NFR-04 | whole artifact | none yet — see Gaps | Quarterly restore drill, timed and logged |
| NFR-05 | `identity`, `transactions` | [`rest-conventions.md`](../07-api/rest-conventions.md) — the 401/403 rules | Automated authorization suite, in CI on every pull request |
| NFR-06 | `identity` | [`security-policy.md`](../00-governance/security-policy.md) — password storage | Automated test asserting stored vs. plaintext password |
| NFR-07 | whole artifact | [`definition-of-done.md`](../00-governance/definition-of-done.md) — the coverage threshold | CI coverage gate on every pull request |
| NFR-08 | whole artifact | [`local-setup.md`](../10-devops/local-setup.md) — the prerequisites and steps | Timed onboarding run, signed off by the tech lead |
| NFR-09 | whole artifact | [`observability.md`](../13-operations/observability.md) — the structured log fields | Log sample reviewed at every release |
| NFR-10 | `transactions` | [`observability.md`](../13-operations/observability.md) — the audit trail lookup | Quarterly support drill, timed |

Every row above is proved by a gate, a monitor, a timed run or a scheduled drill
rather than by a story scenario, and some of them run outside the three layers in
[`testing-strategy.md`](../11-quality/testing-strategy.md), which says exactly
that of **NFR-01**. Each still earns a `TC-NNN` copied from
[`_template-test-case.md`](../11-quality/_template-test-case.md), with that check
written into its Steps.

## Gaps

A gap is an `HU-NN` or `NFR-NN` with no row above, or a row whose design
document or test case does not exist. Three are open:

- **NFR-04** sets a 30-minute restore target that no document in this
  repository describes a procedure for.
- **HU-01** and **HU-07** have an API contract but no screen in `12-ux-ui/` —
  the flow diagrams this project started from do not cover sign-in or
  budgeting. HU-01 is ⭐-priority; its screen should exist before it is
  scheduled into a sprint, or the sprint should record that V1 signs in
  through the API alone.

Once a real test case exists for a row, its Test case column should carry
that test case's `Id` from [`_template-test-case.md`](../11-quality/_template-test-case.md), in place of the layer or check named above.

---

**Related:** [`./user-stories.md`](./user-stories.md) · [`./non-functional.md`](./non-functional.md)
