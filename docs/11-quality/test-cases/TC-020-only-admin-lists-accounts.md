# TC-020 — Only an Administrator Lists the Accounts

## Identification

| Field | Value |
|---|---|
| Id | TC-020 |
| Covers | `HU-09` |
| Layer | `end-to-end` |
| Module | `identity` |

## Preconditions

- One account with the role `ADMIN` and one with the role `USER` exist.

## Steps

1. Call `GET /users` with no `Authorization` header.
2. Call `GET /users` with the `USER` account's token.
3. Call `GET /users` with the `ADMIN` account's token.
4. Call `POST /users` without a token, and as the `USER` account.

## Expected result

1. `401`, with the error `UNAUTHORIZED`.
2. `403`, with the error `FORBIDDEN`.
3. `200` with both accounts ordered by email, each with `role` and no password
   or hash.
4. `401` and `403`; no account is created.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../07-api/contracts/openapi/identity.yaml`](../../07-api/contracts/openapi/identity.yaml)
