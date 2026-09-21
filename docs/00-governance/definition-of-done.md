# 00 — Definition of Done

A story is done when every item below is true, not when the code compiles.

## Checklist

| # | Criterion | How it is verified |
|---|---|---|
| 1 | Code implements every acceptance criterion | Reviewer replays them against the running application |
| 2 | Unit test coverage on changed files meets the threshold below | CI coverage gate on the pull request |
| 3 | No new coupling between modules that were not already coupled | Reviewer checks the import graph, not only the diff |
| 4 | Public module boundaries are unchanged, or the change is documented | Diff against [`../09-modules/module-catalog.md`](../09-modules/module-catalog.md) |
| 5 | Database migrations run cleanly on a copy of the production schema | CI migration job in the `db` repository, not a local run |
| 6 | Logs and errors follow the project's observability format | Reviewer checks the log line against the fields in [`../13-operations/observability.md`](../13-operations/observability.md) |
| 7 | Documentation affected by the change is updated in a linked `docs` pull request, merged no later than the code | Reviewer confirms the link in the code pull request description |
| 8 | At least one reviewer approved and CI is green | Branch protection rule on `main` and `dev` in the code repositories |
| 9 | Every pull request the story opened across repositories is merged | The pull requests link one another; none is left open |

## Coverage threshold

- **Line coverage:** 80% minimum on changed files — the files a pull request touches.
- **The `transactions` module:** 90% minimum, on that same changed-file basis — **NFR-07**.
- Coverage that drops below threshold blocks the merge — it is not a warning.

---

**Related:** [`definition-of-ready.md`](./definition-of-ready.md) · [`README.md`](./README.md)
