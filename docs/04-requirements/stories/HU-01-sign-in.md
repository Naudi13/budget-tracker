# HU-01 — Sign In

## Story

**As** a person with an active account
**I want** to sign in with my email and password
**so that** only I can use my access to the business's financial records

## Acceptance criteria

```gherkin
Scenario: There is no public sign-up
  Given I am not signed in
  When  I open the sign-in screen
  Then  no option offers to create an account
  And   an unauthenticated POST /users is refused with 401

Scenario: A pending account cannot sign in
  Given the account "owner@example.com" has not been activated
  When  I sign in with that email and any password
  Then  I see "El correo o la contraseña no son correctos."

Scenario: Sign in with valid credentials
  Given an account exists with email "owner@example.com"
  When  I sign in with that email and its password
  Then  I reach the dashboard
  And   every later request carries my bearer token

Scenario: Reject a sign-in with a wrong password
  Given an account exists with email "owner@example.com"
  When  I sign in with that email and a wrong password
  Then  I stay on the sign-in screen
  And   I see "El correo o la contraseña no son correctos."

Scenario: Reject a write without signing in (NFR-05)
  Given I am not signed in
  When  any request creates, changes or deletes a category, subcategory, transaction or budget
  Then  the request is refused with 401 and nothing is stored
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `identity` |
| Applicable NFR(s) | `NFR-05`, `NFR-06` |
| Business rule(s) touched | none |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`identity.yaml`](../../07-api/contracts/openapi/identity.yaml) — `POST /sessions` |
| Screen | [`sign-in.md`](../../12-ux-ui/screens/sign-in.md) |
| Test cases | `TC-013`, `TC-014` |
| Blocked by | none — every other story is blocked by this one |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] Sign-out is client-side only (the token is discarded); there is no `DELETE /sessions` in V1

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/module-boundaries.md`](../../02-domain/module-boundaries.md)
