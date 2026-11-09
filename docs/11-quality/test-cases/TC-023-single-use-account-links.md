# TC-023 — An Account Link Works Once and Expires

## Identification

| Field | Value |
|---|---|
| Id | TC-023 |
| Covers | `HU-10` |
| Layer | `end-to-end` |
| Module | `identity` |

## Preconditions

- A pending account with a valid activation link.

## Steps

1. `POST /account-links/confirm` with the token and a valid password, then sign in.
2. Confirm the same token again.
3. As `ADMIN`, `POST /users/{id}/password-resets` and confirm it with a new password.
4. Sign in with the old password, then the new one.
5. Confirm an expired token.
6. Read `identity.account_tokens` directly.

## Expected result

1. `204`, then `201`.
2. `400` on the field `token`.
3. `201`, then `204`.
4. `401`, then `201`.
5. The same `400` as step 2.
6. No row holds the plain token.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md)
