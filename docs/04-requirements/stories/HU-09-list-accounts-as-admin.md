# HU-09 — List the Accounts as an Administrator

## Story

**As** an administrator
**I want** to see the accounts registered in the application
**so that** I know who uses it, without opening any business's data

## Acceptance criteria

```gherkin
Scenario: An administrator lists the accounts
  Given I am signed in with the role ADMIN
  When  I open "Usuarios"
  Then  I see each account's name, email and role, ordered by email

Scenario: No password leaves the API
  Given I am signed in with the role ADMIN
  When  the accounts are listed
  Then  no response field carries a password or its hash

Scenario: Another role is refused
  Given I am signed in with the role USER
  When  I call GET /users
  Then  the API answers 403 with the error FORBIDDEN
  And   the portal does not offer "Usuarios"

Scenario: No token is refused
  Given I send no Authorization header
  When  I call GET /users
  Then  the API answers 401

Scenario: Nobody signs up as an administrator
  Given I sign up sending the role ADMIN in the body
  When  I sign in with that account
  Then  my role is USER

Scenario: The role does not open another account's data
  Given I am signed in with the role ADMIN
  When  I ask for a budget owned by another account
  Then  the API answers 404
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `identity` |
| Applicable NFR(s) | `NFR-05`, `NFR-06` |
| Business rule(s) touched | none |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`identity.yaml`](../../07-api/contracts/openapi/identity.yaml) — `GET /users`, and `role` in `POST /sessions` |
| Screen | [`users.md`](../../12-ux-ui/screens/users.md) |
| Test cases | `TC-020` |
| Blocked by | `HU-01` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`ADR-007`](../../05-architecture/decisions/records/ADR-007-two-roles-in-the-token.md)
