# 10 — Environments

## Environments

| Environment | Purpose | Data source | Who deploys |
|---|---|---|---|
| `local` | One developer's own machine, per [`local-setup.md`](./local-setup.md) | Seed data created locally | The developer, manually |
| `staging` | Proves the artifact before real users see it; where the `end-to-end` layer runs | An anonymized copy of production | The pipeline, automatically, on every cherry-pick into `qa` |
| `production` | Serves real users | The live database | The pipeline, on every cherry-pick into `main`, after manual approval |

## Configuration per environment

| Environment | Values come from | Rule |
|---|---|---|
| `local` | [`.env.example`](../.env.example), copied to `.env` | Never committed — see [`security-policy.md`](../00-governance/security-policy.md) |
| `staging`, `production` | The deployment platform's secret store | Never a file in this repository, not even encrypted |

No environment hardcodes a value that another environment sets
differently — a value that never changes across the row above belongs in
the application's own defaults, not in per-environment configuration.

## Reaching an environment

Nobody reaches `production` directly to fix something. A fix lands on
`develop`, is cherry-picked into `qa` to prove itself in `staging`, and only
then cherry-picked into `main` to reach `production` — the same pipeline every
time, with the `(cherry picked from commit ...)` trailer as the audit trail. A
`hotfix/` branch is the one documented exception: it still goes through the
full pipeline on `main` before the fix is cherry-picked back down to `qa` and
`develop`, per [`git-conventions.md`](../00-governance/git-conventions.md).

---

**Related:** [`./ci-cd.md`](./ci-cd.md) · [`../00-governance/security-policy.md`](../00-governance/security-policy.md)
