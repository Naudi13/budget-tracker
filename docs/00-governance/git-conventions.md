# 00 — Git Conventions

This document is normative — a pull request that violates it is a request for
changes, not a suggestion.

## Repositories

The application is one monolith, but its source lives in four repositories named
`bud-tack-[piece]`: the project abbreviation, then the piece.

| Repository | Holds | Branch flow |
|---|---|---|
| `bud-tack-docs` | This documentation (cross-cutting) | `docs/[description]` to `main` |
| `bud-tack-api` | Backend application and its modules | `develop`, `qa`, `main` |
| `bud-tack-front` | Frontend | `develop`, `qa`, `main` |
| `bud-tack-db` | Schema and migrations | `develop`, `qa`, `main` |

## Branch strategy

Code repositories have three permanent branches. One permanent branch is never
merged into another: a change is promoted only by `git cherry-pick -x`, which
keeps the `(cherry picked from commit ...)` trailer as the audit trail.

| Branch | Kind | Cut from | Reaches |
|---|---|---|---|
| `develop` | Permanent, integration | — | `qa`, by cherry-pick |
| `qa` | Permanent, under test | — | `main`, by cherry-pick |
| `main` | Permanent, production | — | Release |
| `feat/`, `fix/`, `chore/[description]` | Working | `develop` | `develop`, by pull request |
| `hotfix/[description]` | Working | `main` | `main` by pull request, then `qa` and `develop` by cherry-pick |

`bud-tack-docs` has no `develop` or `qa`: `main` is fed only by `docs/[description]`
branches, one per section, for example `docs/04-requirements-update`.

- Nobody authors a commit directly on a permanent branch: only merged pull
  requests land on `develop`, and only cherry-picks land on `qa` and `main`.
- One branch equals one task (in `bud-tack-docs`, one section), and is deleted
  right after merge.

## Branch naming

`[type]/[short-description-in-kebab-case]` — e.g. `feat/transaction-history`, `docs/03-product-update`.

## Commit format and pull request titles

| Type | When to use |
|---|---|
| `feat` | New functionality |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Restructuring without behavior change |
| `test` | Add or change tests |
| `chore` | Tooling, dependencies, CI |
| `perf` | Performance improvement |

Format: `type(scope): imperative, lowercase, no trailing period`. In code
repositories the scope is the module (`feat(transactions): add soft delete`); in
`bud-tack-docs` it is the section (`docs(domain): trace rules to user stories`).
A pull request title follows the same format.

## Merge policy

- **`bud-tack-docs`:** merge commit into `main`; each pull request stays inside one
  section, and is merged before the next opens. The one cross-section edit allowed
  is a sentence in another section that the change would otherwise leave false;
  the description lists every such file.
- **Code repositories:** squash and merge into `develop`, one commit per story.
- **Rebase is not used** on branches more than one person has pulled.
- A pull request needs one approval; in code repositories the pipeline is green too.

## Changes spanning repositories

A story that touches several repositories opens one branch and one pull request
per repository, with the same description and links to the others. Code merges in
the order `bud-tack-db`, `bud-tack-api`, `bud-tack-front`; the documentation pull
request merges no later than the first code pull request.

---

**Related:** [`agile-conventions.md`](./agile-conventions.md) · [`README.md`](./README.md)
