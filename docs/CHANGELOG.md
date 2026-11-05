# Changelog
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | [SemVer](https://semver.org/)

## [Unreleased]

### Added
- `ADR-005`: FluentMigrator, in its own .NET project in `bud-tack-db`, versions the schema

- `04-requirements/stories/`: HU-01 to HU-08, each with Gherkin acceptance criteria and its blockers
- Screens `sign-in.md`, `sign-up.md` and `budgets.md`, closing the HU-01 and HU-07 gaps
- Pipelines for `bud-tack-portal` and `bud-tack-infra` in `10-devops/ci-cd.md`
- `ADR-006`: the portal ships as its own nginx image that proxies `/api/` and `/health`
  to the API, so every environment has one origin and the API sends no CORS headers
- Budgets can be changed and deleted: `PATCH` and `DELETE /budgets/{budgetId}`, rule `BR-06`,
  `TC-019`, and HU-07 and its screen extended to match

### Changed
- `12-ux-ui/design-system.md` takes the BudgetTrack identity manual: the logo and
  its usage rules, and the navy / mint / coral palette, mapped to tokens that
  meet WCAG 2.1 AA (coral action buttons with navy text, navy primary, darkened
  mint for success)
- Aligned with the code of Sprint 0: `_stacks/csharp-aspnet.md` names
  `FluentMigrator.Runner.Postgres`, `BudTack.slnx`, Microsoft.Testing.Platform and
  `dotnet test --project`, the `AddXxxModule()` registration on each module marker,
  and the IDEs that can open .NET 10; `ci-cd.md` uses `ng test --coverage` and the
  real `bud-tack-infra` pipeline; `local-setup.md` and `environments.md` add the
  whole application on port 8080 (`docker compose --profile app`); `.env.example`
  sets `ASPNETCORE_URLS` instead of `APP_PORT`
- Promotion between permanent branches is a pull request (`develop` → `qa` → `main`),
  no longer `git cherry-pick -x`, matching every code repository's README and `CODEOWNERS`
- `ADR-001` to `ADR-005` accepted at the architecture review gate
- Repositories renamed to the real ones: `bud-tack-front` is `bud-tack-portal`, and
  `bud-tack-infra` is added — five repositories, not four
- `10-devops/local-setup.md` runs `bud-tack-infra`, `-db`, `-api` and `-portal` in that order
- Liquibase replaced by FluentMigrator in `06-data/migrations.md`, `_stacks/csharp-aspnet.md`
  and `10-devops/`, so the toolchain is only Angular, .NET and PostgreSQL — no Java runtime
- Migrations moved out of the application tree (`resources/db/migration/`) into `bud-tack-db`
- `10-devops/local-setup.md` clones and runs `bud-tack-db` before `bud-tack-api`

## [2.0.0] - 2026-09-12

### Added
- Full SDD section structure `00`-`13` plus `_stacks/` — 76 files, 15 sections
- `00-sdd-guide.md` as the entry point: four phases, four review gates, and a
  week-by-week fill-in order
- Modular monolith adopted as the default architectural stance, recorded in
  `05-architecture/decisions/records/ADR-001-modular-monolith-as-default.md`
- Boundary-enforcement guidance for four stacks — ArchUnit, eslint-boundaries,
  import-linter and Deptrac
- Per-module documentation template under `09-modules/_template-module/`, copied
  once per module rather than edited in place
- Templates for user story, ADR, test case, screen, sprint retro and incident
- Stack guides for Java/Spring, Node/TypeScript, Python/Django and PHP/Laravel,
  each pinning a version baseline checked against what the build resolves today

### Changed
- The five `docs/0N-*.md` files were absorbed into the numbered sections that now
  own their subject: project structure and layered architecture into
  `05-architecture/`, database conventions into `06-data/`, testing standards into
  `11-quality/`, deployment into `10-devops/`
- Corrected the sibling-framework link: it pointed at the organisation that
  repository used to live in, and resolved only through a redirect. It now
  addresses `jesusarielgb-works` directly
- `README.md` rewritten around explicit scope blocks, a section-dependency diagram
  and a table of all 15 sections
- `CONTRIBUTING.md` rewritten against the section structure that replaced `docs/`
- Every document now follows one anatomy: a numbered title, an instructions block
  that is deleted on completion, content as tables or diagrams, and a related-links
  footer

### Removed
- The `docs/` folder and its five flat documents, superseded by the numbered sections

## [1.0.0] - 2026-08-24
### Added
- Initial framework: project structure, layered architecture, DB conventions, testing, deployment
