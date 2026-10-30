# dashboard — Runbook

## Symptoms this module can cause

| Symptom | Likely cause inside this module |
|---|---|
| KPIs or the recent list look wrong | Rarely this module: it owns no data. Check `transactions`'s runbook first |
| `GetDashboard` is slow | The aggregation query against `transactions.transaction_details`, not a bug in `dashboard` itself — see **NFR-02** |
| `recentTransactions` shows more or fewer than 5 rows | The fixed limit in `internal/application` was changed without updating this contract |

## Diagnosis

| Step | How |
|---|---|
| Isolate the module | Filter application logs for the `dashboard` logger namespace |
| Reproduce | Call `DashboardFacade.GetDashboard` directly from a REPL or test, bypassing the UI |
| Check the data | `dashboard` owns no table; compare its output against a direct query on `transactions.transactions` and `transactions.transaction_details` — see [`../transactions/data-model.md`](../transactions/data-model.md) |

## Recovery

| Situation | Action |
|---|---|
| A figure disagrees with `transactions`'s own data | The defect is in `GetTotals`/`ListTransactions` or in how `dashboard` calls them — never patch the figure directly, there is no row to patch |
| A code defect | Ship a fix through the single release pipeline; there is no way to redeploy this module alone |

There is one process and one release: recovering this module never means
restarting it independently, only the whole application.

## Escalation

| When | Escalate to |
|---|---|
| The fix would touch `transactions`'s own aggregation logic | Tech lead — a cross-module fix needs a facade change, not a workaround |
| The cause is still unknown after diagnosis | Whoever last changed `internal/` here — see git blame |

---

**Related:** [`./README.md`](./README.md) · [`../../13-operations/README.md`](../../13-operations/README.md)
