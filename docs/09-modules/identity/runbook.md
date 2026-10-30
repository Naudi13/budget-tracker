# identity — Runbook

## Symptoms this module can cause

| Symptom | Likely cause inside this module |
|---|---|
| Sign-in rejects a password that should be valid | A `PasswordHasher` version mismatch (iteration count or format changed without migrating existing hashes) |
| Sign-up always rejects as a duplicate email | The uniqueness check or its index is case-sensitive where the business expects case-insensitive emails |
| Every request returns 401, even with a token just issued | `AUTH_JWT_SECRET` rotated without a grace period for tokens already issued |

## Diagnosis

| Step | How |
|---|---|
| Isolate the module | Filter application logs for the `identity` logger namespace |
| Reproduce | Call `IdentityFacade.SignIn` directly from a REPL or test, bypassing the UI |
| Check the data | Query `identity.users` — see [`./data-model.md`](./data-model.md) — never another module's tables |

## Recovery

| Situation | Action |
|---|---|
| Bad data inside `identity.users` | Fix it through `IdentityFacade`, never a direct `UPDATE` — the facade enforces the invariants in [`./data-model.md`](./data-model.md) |
| A code defect | Ship a fix through the single release pipeline; there is no way to redeploy this module alone |

There is one process and one release: recovering this module never means
restarting it independently, only the whole application.

## Escalation

| When | Escalate to |
|---|---|
| The fix would touch another module's data (e.g. backfilling `created_by`) | Tech lead — a cross-module fix needs a facade change, not a workaround |
| The cause is still unknown after diagnosis | Whoever last changed `internal/` here — see git blame |

---

**Related:** [`./README.md`](./README.md) · [`../../13-operations/README.md`](../../13-operations/README.md)
