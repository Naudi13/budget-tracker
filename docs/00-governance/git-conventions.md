# 00 — Git Conventions

This document is normative — a pull request that violates it is a request for
changes, not a suggestion.

## Repositories

The application is one monolith, but its source lives in four repositories.
Every rule below applies to all four unless a row says otherwise.

| Repository | Holds | Branch flow |
|---|---|---|
| `docs` | This documentation | `docs/[description]` to `main` |
| `be` | Backend application and its modules | `[type]/[description]` to `dev` to `main` |
| `fe` | Frontend | `[type]/[description]` to `dev` to `main` |
| `db` | Schema and migrations | `[type]/[description]` to `dev` to `main` |

## Branch strategy

Code repositories (`be`, `fe`, `db`):

```
main                    Production. Always deployable. Merges only from dev at release time.
  |-- dev               Integration branch. Merges from the branches below.
        |-- feat/[description]    One branch per feature or user story
        |-- fix/[description]     One branch per bug fix
        |-- chore/[description]   Tooling and dependencies
        |-- hotfix/[description]  Urgent fix branched from main
```

The `docs` repository has no `dev`: `main` is fed only by `docs/[description]`
branches, one per section, for example `docs/04-requirements-update`.

- Nobody commits directly to `main` or `dev`.
- One branch equals one task; in `docs`, one branch equals one section.
- Branches are deleted right after merge.

## Branch naming

`[type]/[short-description-in-kebab-case]` — examples: `feat/transaction-history`
in a code repository, `docs/03-product-update` in `docs`.

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

Format: `type(scope): imperative, lowercase, no trailing period`. In the code
repositories the scope is the module name (`feat(transactions): add soft delete`);
in `docs` it is the section (`docs(domain): trace business rules to user stories`).
A pull request title follows the same format.

## Merge policy

- **`docs`:** merge commit into `main`, one section per pull request. Merge a
  pull request before opening the next, so each diff shows only its own section.
- **Code repositories:** squash and merge into `dev` — one commit per story; merge
  commit from `dev` into `main` at release time to keep release history.
- **Rebase is not used** on branches more than one person has pulled.
- A pull request needs one approval; in the code repositories the pipeline must
  also be green.

## Changes spanning repositories

A story that touches several repositories opens one branch and one pull request
per repository, with the same description on each and each linking the others.
Code merges in the order `db`, `be`, `fe`; the `docs` pull request merges no
later than the first code pull request.

---

**Related:** [`agile-conventions.md`](./agile-conventions.md) · [`README.md`](./README.md)
