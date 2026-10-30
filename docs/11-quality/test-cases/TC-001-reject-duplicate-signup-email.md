# TC-001 — Reject Sign-Up with a Duplicate Email

## Identification

| Field | Value |
|---|---|
| Id | TC-001 |
| Covers | `HU-01` |
| Layer | `unit` |
| Module | `identity` |

## Preconditions

- A user already exists with email `owner@example.com`.

## Steps

1. Call `IdentityFacade.SignUpAsync` with name "New Owner", email
   `owner@example.com`, and a valid password.

## Expected result

The call fails with a duplicate-email error; no new row is inserted into
`identity.users`. At the API, this maps to `409 Conflict` with error code
`CONFLICT`, per [`rest-conventions.md`](../../07-api/rest-conventions.md).

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../04-requirements/traceability-matrix.md`](../../04-requirements/traceability-matrix.md)
