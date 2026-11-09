# TC-021 — Rename a Category, Keeping Its Name Unique

## Identification

| Field | Value |
|---|---|
| Id | TC-021 |
| Covers | `HU-03` |
| Layer | `end-to-end` |
| Module | `transactions` |

## Preconditions

- An `ADMIN` is signed in, and the expense categories `Oficina` and `Arriendo` exist.

## Steps

1. `PATCH /categories/{id}` of `Oficina` with `{ "name": "Papelería" }`.
2. `PATCH` the same category with `{ "name": "Arriendo" }`.
3. `PATCH` the same category with `{ "name": "   " }`.
4. `PATCH` the same category with `{}`.
5. `PATCH` the same category as a `USER`.

## Expected result

1. `200`, with the new name, the same type and the same state.
2. `409 CONFLICT`; the name does not change.
3. `400 VALIDATION_ERROR` on `name`.
4. `400 VALIDATION_ERROR`.
5. `403 FORBIDDEN`.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../07-api/contracts/openapi/categories.yaml`](../../07-api/contracts/openapi/categories.yaml)
