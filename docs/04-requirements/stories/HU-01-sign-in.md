# HU-01 — Sign In to a Business Account

## Story

**As** a business owner
**I want** to create my business's account once and sign in to it with my email and password
**so that** only I can read and change my business's financial records

## Acceptance criteria

```gherkin
Scenario: Sign up a new business account
  Given no account exists with email "owner@example.com"
  When  I sign up with name "Ana Owner", that email and an 8-character password
  Then  the account is created and I am taken to the sign-in screen

Scenario: Reject a sign-up with an email already in use
  Given an account exists with email "owner@example.com"
  When  I sign up again with that email
  Then  the account is not created
  And   I see "An account already uses this email. Sign in instead."

Scenario: Sign in with valid credentials
  Given an account exists with email "owner@example.com"
  When  I sign in with that email and its password
  Then  I reach the dashboard
  And   every later request carries my bearer token

Scenario: Reject a sign-in with a wrong password
  Given an account exists with email "owner@example.com"
  When  I sign in with that email and a wrong password
  Then  I stay on the sign-in screen
  And   I see "The email or password is not correct."

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
| Contract | [`identity.yaml`](../../07-api/contracts/openapi/identity.yaml) — `POST /users`, `POST /sessions` |
| Screens | [`sign-up.md`](../../12-ux-ui/screens/sign-up.md) · [`sign-in.md`](../../12-ux-ui/screens/sign-in.md) |
| Test cases | `TC-001`, `TC-013`, `TC-014` |
| Blocked by | none — every other story is blocked by this one |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

- [ ] Sign-out is client-side only (the token is discarded); there is no `DELETE /sessions` in V1

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`../../02-domain/module-boundaries.md`](../../02-domain/module-boundaries.md)
