# HU-13 — Inactivate or Delete an Account

## Story

**As** an administrator
**I want** to turn off the account of someone who no longer works here, or remove one that was never used
**so that** only current people can get in, while the history keeps who did what

## Acceptance criteria

```gherkin
Scenario: Inactivate an account
  Given the account of Ana is active and she is signed in
  When  I inactivate it and confirm
  Then  it is listed as "Inactiva"
  And   her next request is answered with 401
  And   she cannot sign in

Scenario: The history keeps the account
  Given Ana recorded a transaction and her account was inactivated
  When  I open the transaction history
  Then  the transaction still names her

Scenario: Activate it again
  Given the account of Ana is inactive
  When  I activate it
  Then  she can sign in with her password

Scenario: An administrator cannot inactivate their own account
  Given I am signed in with the role ADMIN
  When  I try to inactivate my own account
  Then  the API answers 409 and nothing changes
  And   the portal does not offer it

Scenario: Delete an account that was never activated
  Given the account of Ana is pending
  When  I delete it and confirm
  Then  it is no longer listed
  And   its activation link stops working

Scenario: An account with a password cannot be deleted
  Given the account of Ana was activated
  When  a request deletes it
  Then  the API answers 409 and it stays

Scenario: Another role is refused
  Given I am signed in with the role USER
  When  I inactivate or delete an account
  Then  the API answers 403
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `identity` |
| Applicable NFR(s) | `NFR-05` |
| Business rule(s) touched | `BR-09` |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`identity.yaml`](../../07-api/contracts/openapi/identity.yaml) — `PATCH` and `DELETE /users/{userId}` |
| Screen | [`users.md`](../../12-ux-ui/screens/users.md) |
| Test case | `TC-027` |
| Blocked by | `HU-09` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`ADR-011`](../../05-architecture/decisions/records/ADR-011-check-the-account-on-every-request.md)
