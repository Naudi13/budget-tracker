# 00 — Agile Conventions

This project runs two-week sprints, on a single backlog, for one application
whose source lives in four repositories (`docs`, `be`, `fe`, `db`), built by one
team.

## Ceremonies

| Ceremony | Cadence | Artifact produced |
|---|---|---|
| Backlog refinement | Weekly, 1 hour | Stories move to "Ready" per [`definition-of-ready.md`](./definition-of-ready.md) |
| Sprint planning | Every 2 weeks, 2 hours | Sprint goal and a committed story list |
| Daily standup | Daily, 15 minutes | Blockers logged in the tracker, not only spoken |
| Sprint review / demo | Every 2 weeks, 1 hour | Stakeholder feedback recorded on the story |
| Sprint retrospective | Every 2 weeks, 1 hour | Filled copy of [`_template-sprint-retro.md`](./_template-sprint-retro.md) |
| Backlog grooming with Product Owner | Bi-weekly, 30 minutes | Reprioritized backlog, top 10 items estimated |

## Estimation

- **Unit:** story points, on a Fibonacci-like scale (1, 2, 3, 5, 8, 13).
- **Who estimates:** the whole team, through planning poker — not the tech lead alone.
- **Re-estimation:** allowed only before a story enters "Ready."

## Backlog tool

The backlog lives on a single board with columns `Backlog -> Ready -> In progress
-> In review -> Done`. A story skips no column, even when it looks trivial.

One board covers all four repositories: a story that touches several is one card,
linked to its pull request in each repository, and it reaches "Done" only when
every one of those pull requests is merged.

---

**Related:** [`definition-of-ready.md`](./definition-of-ready.md) · [`README.md`](./README.md)
