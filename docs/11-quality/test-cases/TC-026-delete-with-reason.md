# TC-026 — A Delete Needs a Reason, and Only an Administrator Reviews It

## Identification

| Field | Value |
|---|---|
| Id | TC-026 |
| Covers | `HU-06` |
| Layer | `end-to-end` |
| Module | `transactions` |

## Preconditions

- `USER` A recorded a transaction, and `USER` B is signed in.

## Steps

1. As A, delete it with the reason `x`.
2. As B, delete it with a valid reason.
3. As A, delete it with a valid reason.
4. As A, `GET /transactions?deleted=true`.
5. As `ADMIN`, `GET /transactions?deleted=true`.

## Expected result

1. `400` on the field `reason`.
2. `404`.
3. `204`.
4. `403`.
5. `200`, with who deleted it, when and why.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md)
