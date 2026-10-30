# TC-013 — Write Endpoints Reject an Unauthenticated Request

## Identification

| Field | Value |
|---|---|
| Id | TC-013 |
| Covers | `NFR-05` |
| Layer | `module-integration` |
| Module | `identity`, `transactions` |

## Preconditions

- None: no `Authorization` header is sent.

## Steps

1. Call every write endpoint — `POST`/`PATCH`/`DELETE` on transactions,
   categories, subcategories, and budgets — with no `Authorization` header.

## Expected result

Every call responds `401 Unauthorized`; none performs the write.
`POST /users` and `POST /sessions` are excluded, per
[`rest-conventions.md`](../../07-api/rest-conventions.md).

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../00-governance/security-policy.md`](../../00-governance/security-policy.md)
