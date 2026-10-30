# transactions — Runbook

## Symptoms this module can cause

| Symptom | Likely cause inside this module |
|---|---|
| Recording a transaction fails with no clear reason | BR-02 rejected it: the details mix income and expense subcategories |
| A category or subcategory cannot be deleted | BR-05: it still owns a subcategory or a budget, or a detail still references it — deactivate it instead |
| A transaction a user expected to see is missing from history | It was soft-deleted (BR-04); check `deleted_at` directly, the facade excludes it from listings by design |
| Dashboard totals look wrong | Check this module's data first — `dashboard` only aggregates what is here |

## Diagnosis

| Step | How |
|---|---|
| Isolate the module | Filter application logs for the `transactions` logger namespace |
| Reproduce | Call `TransactionsFacade` directly from a REPL or test, bypassing the UI |
| Check the data | Query this module's own tables — see [`./data-model.md`](./data-model.md) — never another module's |

## Recovery

| Situation | Action |
|---|---|
| Bad data inside this module's tables | Fix it through `TransactionsFacade`, never a direct `UPDATE` — the facade enforces the invariants in [`./data-model.md`](./data-model.md) |
| A code defect | Ship a fix through the single release pipeline; there is no way to redeploy this module alone |

There is one process and one release: recovering this module never means
restarting it independently, only the whole application.

## Escalation

| When | Escalate to |
|---|---|
| The fix would touch `identity.users` (e.g. a `created_by` backfill) | Tech lead — a cross-module fix needs a facade change, not a workaround |
| The cause is still unknown after diagnosis | Whoever last changed `internal/` here — see git blame |

---

**Related:** [`./README.md`](./README.md) · [`../../13-operations/README.md`](../../13-operations/README.md)
