# TC-027 — Inactivate or Delete an Account

## Identification

| Field | Value |
|---|---|
| Id | TC-027 |
| Covers | `HU-13` |
| Layer | `end-to-end` |
| Module | `identity` |

## Preconditions

- An `ADMIN` is signed in. `USER` A is active and signed in. `USER` B is pending.

## Steps

1. `PATCH /users/{A}` with `{ "isActive": false }`.
2. As A, with the token it already had, `GET /categories`.
3. `POST /sessions` as A.
4. `PATCH /users/{A}` with `{ "isActive": true }`, then `POST /sessions` as A.
5. `PATCH /users/{admin}` with `{ "isActive": false }`, as that administrator.
6. `DELETE /users/{A}`.
7. `DELETE /users/{B}`, then confirm B's activation link.
8. `PATCH` and `DELETE /users/{B}` as A.

## Expected result

1. `200`, `status` is `INACTIVE`.
2. `401`.
3. `401`.
4. `200`, `status` is `ACTIVE`, then `201`.
5. `409`.
6. `409`.
7. `204`, then `400`.
8. `403`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../07-api/contracts/openapi/identity.yaml`](../../07-api/contracts/openapi/identity.yaml)
