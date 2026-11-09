# TC-022 — An Administrator Creates a Pending Account

## Identification

| Field | Value |
|---|---|
| Id | TC-022 |
| Covers | `HU-09` |
| Layer | `end-to-end` |
| Module | `identity` |

## Preconditions

- An `ADMIN` is signed in.

## Steps

1. `POST /users` with a name, an email and the role `USER`.
2. `POST /sessions` with that email and any password.
3. `POST /users/{id}/activation-links`, then use the first link.

## Expected result

1. `201`, `status` is `PENDING`, with an activation link and no password.
2. `401`.
3. The second link is returned; the first one answers `400`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md)
