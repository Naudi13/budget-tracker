# bud-tack-portal

> Budget Tracker - Web portal: user interface that consumes the API

Part of the **Budget Tracker** full-stack project — team `budget-tracker`, Complementaria III - Desarrollo Fullstack 2026-B.
Governance and documentation live in [`bud-tack-docs`](https://github.com/code-corhuila/bud-tack-docs).

## Run it locally

Needs Node.js 22 LTS (`package.json` `engines`) and the API running on
`http://localhost:3000` (`bud-tack-api`, `10-devops/local-setup.md`).

```bash
npm ci
npm start                                  # http://localhost:4200
```

`ng serve` forwards `/api/` and `/health` to `localhost:3000` through
`proxy.conf.json`, so the browser sees one origin and the API needs no CORS
(ADR-006). Without a token `/` goes to sign-in; `/sign-up` creates the
account (HU-01). `/status` says whether the API and its database are up.

| Route | Who | Screen |
|---|---|---|
| `/sign-in`, `/sign-up` | Signed out only | `12-ux-ui/screens/sign-in.md`, `sign-up.md` |
| `/dashboard` (and `/`) | Signed in only | Placeholder until HU-08 |
| `/settings/categories` | Signed in only | `categories-configuration.md`: categories and subcategories (HU-02, HU-03) |
| `/status` | Anyone | `GET /health` through the proxy |

The token lives in memory and `sessionStorage`. Sign-out discards it; a `401`
from the API discards it too and goes to sign-in.

## Language

Everything the user sees is in Spanish (`<html lang="es">`, locale `es-CO`
for dates, numbers and money), written with *tuteo*, as the BudgetTrack
identity manual asks. Code, names, comments, tests' names and commits stay in
English. The API's error `message` is English and meant for developers: a
screen never shows it, it picks its own Spanish text from the `status`, the
`error` code and the `details[].field` of the response.

## Layout

| Path | Holds |
|---|---|
| `src/app/app.*` | The shell: header, skip link, one `<main>` with the router outlet |
| `src/app/app.routes.ts` | One route per screen of `12-ux-ui/screens/` |
| `src/app/core/` | HTTP wiring shared by every screen: the `{ error, message, details }` mapping, the error, bearer-token and 401 interceptors, the token store, `IdentityService` and the route guards |
| `src/app/pages/` | One folder per screen |
| `src/styles.css` | The tokens of `12-ux-ui/design-system.md` and the button styles |
| `nginx/default.conf.template` | The production server: Angular files plus the `/api/` and `/health` proxy |

## Pipeline

```bash
npm ci
npm run lint                               # ng lint
npm run test:ci                            # ng test, coverage at least 80 % (NFR-07)
npm run build                              # ng build, production
docker build -t bud-tack-portal:local .    # package: nginx image on port 8080
```

The image serves the files on port 8080 as a non-root user and forwards
`/api/` and `/health` to `API_ORIGIN` (default `http://api:3000`, the service
name in `bud-tack-infra`'s Compose file).

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
