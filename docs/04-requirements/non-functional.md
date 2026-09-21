# 04 — Non-Functional Requirements

## What makes a requirement here valid

| Not this | This |
|---|---|
| The system must be fast | p95 under 300 ms at 50 concurrent users |
| The system must be secure | Zero write endpoints accept an unauthenticated request |

## Requirements

| NFR-NN | Category | Threshold | How it is measured |
|---|---|---|---|
| NFR-01 | Performance | p95 response time under 300 ms at 50 concurrent users, on transaction and dashboard read endpoints | Scripted load test against staging before each release |
| NFR-02 | Performance | Dashboard KPIs and the 5 most recent transactions render in under 2 seconds for a business with up to 10,000 transactions | Timed run against a seeded staging dataset before release |
| NFR-03 | Availability | 99.5% uptime per month, measured across the published business-hours support window | Uptime monitor polling `/health` every 5 minutes |
| NFR-04 | Availability | Database restore from the latest backup completes in under 30 minutes | Quarterly restore drill, timed and logged |
| NFR-05 | Security | 100% of write endpoints (transaction, category, subcategory, budget) reject an unauthenticated request | Automated authorization test suite, run in CI on every pull request |
| NFR-06 | Security | 100% of stored passwords are a salted hash; the plaintext value is never persisted or logged | Automated test asserting the stored value never matches the plaintext input |
| NFR-07 | Maintainability | Unit test coverage at least 80% on changed files, at least 90% on the `transactions` module | CI coverage gate, see [`definition-of-done.md`](../00-governance/definition-of-done.md) |
| NFR-08 | Maintainability | A developer runs the application locally in under 1 hour, start to first successful request | Timed onboarding checklist signed off by the tech lead |
| NFR-09 | Observability | 100% of unhandled errors are logged with a request id and a timestamp | Manual log sample reviewed at every release |
| NFR-10 | Observability | A soft-deleted transaction's `deleted_by` and `deleted_at` are found in the audit trail in under 5 minutes | Quarterly support drill, timed |

## Identifier format

`NFR-NN` — same two-digit, sequential, never-reused convention as `HU-NN`.

---

**Related:** [`../00-governance/documentation-rules.md`](../00-governance/documentation-rules.md) · [`./traceability-matrix.md`](./traceability-matrix.md)
