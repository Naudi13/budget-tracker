# 10 — Environments

## Environments

| Environment | Purpose | Data source | Who deploys |
|---|---|---|---|
| `local` | One developer's own machine, per [`local-setup.md`](./local-setup.md) | Seed data created locally | The developer, manually |
| `staging` | Proves the artifact before real users see it; where the `end-to-end` layer runs | An anonymized copy of production | The pipeline, automatically, on every merged pull request into `qa` |
| `production` | Serves real users | The live database | The pipeline, on every merged pull request into `main`, after manual approval |

## Configuration per environment

| Environment | Values come from | Rule |
|---|---|---|
| `local` | [`.env.example`](../.env.example), copied to `.env` | Never committed — see [`security-policy.md`](../00-governance/security-policy.md) |
| `staging`, `production` | The deployment platform's secret store | Never a file in this repository, not even encrypted |

No environment hardcodes a value that another environment sets
differently — a value that never changes across the row above belongs in
the application's own defaults, not in per-environment configuration.

## One origin in every environment

The browser reaches one origin only, per
[`ADR-006`](../05-architecture/decisions/records/ADR-006-portal-image-behind-reverse-proxy.md):

| Environment | Serves the Angular files | Forwards `/api/` and `/health` to the API |
|---|---|---|
| `local` | `ng serve` on port 4200 | The Angular dev server's `proxy.conf.json`, to `localhost:3000` |
| `local`, whole application | The `bud-tack-portal` nginx image on port 8080 (`docker compose up` in `bud-tack-infra`) | The same nginx, to the API container — the same shape as `staging` |
| `staging`, `production` | The `bud-tack-portal` nginx image | The same nginx, to the API container, on `bud-tack-infra`'s Compose network |

The same nginx also forwards `/swagger-ui.html` and `/swagger/`. The API exposes no
port of its own outside that network, and its CORS policy admits only
`CORS_ALLOWED_ORIGIN`, per [`ADR-008`](../05-architecture/decisions/records/ADR-008-bcrypt-cors-and-swagger.md).

## Reaching an environment

Nobody reaches `production` directly to fix something. A fix lands on
`develop`, reaches `qa` by a promotion pull request to prove itself in
`staging`, and only then reaches `main` by a release pull request to get to
`production` — the same pipeline every time, with the merged pull requests as
the audit trail. A `hotfix/` branch is the one documented exception: its pull
request into `main` still runs the full pipeline, and a second pull request from
the same branch brings the fix back to `develop`, per
[`git-conventions.md`](../00-governance/git-conventions.md).

---

**Related:** [`./ci-cd.md`](./ci-cd.md) · [`../00-governance/security-policy.md`](../00-governance/security-policy.md)
