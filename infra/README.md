# bud-tack-infra

> Budget Tracker - Infrastructure: Compose/IaC, environments and secrets

Part of the **Budget Tracker** full-stack project — team `budget-tracker`, Complementaria III - Desarrollo Fullstack 2026-B.
Governance and documentation live in [`bud-tack-docs`](https://github.com/code-corhuila/bud-tack-docs).

## Local PostgreSQL

The one database of the application, with the two roles that
`00-governance/security-policy.md` requires. Needs Docker with Compose.

```bash
cp .env.example .env
docker compose up -d --wait
sh scripts/verify-roles.sh
```

| Role | Password (local) | Used by | Can |
|---|---|---|---|
| `bud_tack_migrator` | `dev` | `bud-tack-db` (`MIGRATOR_DATABASE_URL`) | Own the database `app_dev`, create schemas and tables |
| `bud_tack_app` | `dev` | `bud-tack-api` (`DATABASE_URL`) | Connect only; table rights come from the migrations |

The roles are created by `init/01-roles.sh` on the **first** start only. After
changing a password in `.env`, recreate the volume: `docker compose down --volumes`,
then `up` again. The schemas `identity` and `transactions` are not created here:
they are the first migration in `bud-tack-db` (ADR-005).

## Branching

Three permanent branches. **None of them accepts a direct commit** — you enter through a child
branch and leave through a Pull Request.

```
develop  <--PR--  feat/... fix/... chore/...
qa       <--PR--  develop
main     <--PR--  qa (release)   hotfix/...
```

`develop` is the default branch: start every task from it. `main` requires **1 approval from
`ariel5253`**. On `develop` and `qa` the team sets its own review rule.

Commit and branch naming: `00-governance/git-conventions.md` in `bud-tack-docs`.
