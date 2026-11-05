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
(ADR-006). The start page calls `/health` and says whether the API and its
database are up; sign-in replaces it with HU-01.

## Layout

| Path | Holds |
|---|---|
| `src/app/app.*` | The shell: header, skip link, one `<main>` with the router outlet |
| `src/app/app.routes.ts` | One route per screen of `12-ux-ui/screens/` |
| `src/app/core/` | HTTP wiring shared by every screen: the `{ error, message, details }` mapping, the error and bearer-token interceptors, the token store |
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
