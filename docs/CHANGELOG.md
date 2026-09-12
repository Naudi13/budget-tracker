# Changelog
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | [SemVer](https://semver.org/)

## [Unreleased]

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
- Stack guides for Java/Spring, Node/TypeScript, Python/Django and PHP/Laravel

### Changed
- The five `docs/0N-*.md` files were absorbed into the numbered sections that now
  own their subject: project structure and layered architecture into
  `05-architecture/`, database conventions into `06-data/`, testing standards into
  `11-quality/`, deployment into `10-devops/`
- Sibling-framework link moved to the `jesusarielgb-works` organisation; it had
  pointed at an organisation this repository no longer belongs to and resolved
  only through a redirect
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
