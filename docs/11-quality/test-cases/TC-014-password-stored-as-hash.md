# TC-014 — Password Is Stored Only as a Salted Hash

## Identification

| Field | Value |
|---|---|
| Id | TC-014 |
| Covers | `NFR-06` |
| Layer | `unit` |
| Module | `identity` |

## Preconditions

- None.

## Steps

1. Call `POST /users` with password `Str0ngPass!23`.
2. Read the new row's `password_hash` column directly through persistence.

## Expected result

`password_hash` does not equal the plaintext password and has BCrypt's format,
starting with `$2` and naming cost 12. No log line captured during
the call contains the plaintext password.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../00-governance/security-policy.md`](../../00-governance/security-policy.md)
