# dashboard — Decisions

## [DEC-01] No caching layer for V1

| Field | Value |
|---|---|
| Date | 2026-09-21 |
| Status | `Active` |

**Context:** `GetDashboard` recomputes totals from `transactions` on every
call. `NFR-02` only requires rendering under 2 seconds at up to 10,000
transactions, which an indexed query already meets without a cache.

**Decision:** Ship V1 with no cache in front of `TransactionsFacade`. Revisit
only if `NFR-02`'s timed run fails against a larger seeded dataset.

**Global ADR, if any:** none — internal to this module; adding a cache later
does not change the public API.

## Adding the next decision

Copy the `[DEC-01]` block above, renumbered `[DEC-02]`, and add it below this
heading — newest last, so the file reads in the order decisions were made.

---

**Related:** [`./README.md`](./README.md) · [`../../05-architecture/decisions/README.md`](../../05-architecture/decisions/README.md)
