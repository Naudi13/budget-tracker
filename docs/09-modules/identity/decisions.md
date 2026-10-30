# identity — Decisions

## [DEC-01] Password hashing with ASP.NET Core Identity's `PasswordHasher`

| Field | Value |
|---|---|
| Date | 2026-10-30 |
| Status | `Active` |

**Context:** `NFR-06` requires every stored password to be a salted hash, but
no document fixed the algorithm.

**Decision:** Hash passwords with ASP.NET Core Identity's built-in
`PasswordHasher<TUser>` (PBKDF2 with HMAC-SHA256, a random salt per
password). It ships with the framework chosen in
[`ADR-002`](../../05-architecture/decisions/records/ADR-002-angular-dotnet-postgresql-stack.md),
so it needs no extra dependency, and it already satisfies `NFR-06` without
further configuration.

**Global ADR, if any:** none — internal to this module; it does not change
`identity`'s public API or reach beyond it.

## Adding the next decision

Copy the `[DEC-01]` block above, renumbered `[DEC-02]`, and add it below this
heading — newest last, so the file reads in the order decisions were made.

---

**Related:** [`./README.md`](./README.md) · [`../../05-architecture/decisions/README.md`](../../05-architecture/decisions/README.md)
