# HU-09 — Administer the Accounts

## Story

**As** an administrator
**I want** to create the accounts and see who uses the application
**so that** only the people I choose can get in

## Acceptance criteria

```gherkin
Scenario: The first administrator exists from the start
  Given a new installation
  Then  one ADMIN exists, created by the migrations, and nobody had to sign up

Scenario: Create an account
  Given I am signed in with the role ADMIN
  When  I create "Ana" with her email and the role USER
  Then  the account is listed as "Pendiente"
  And   I get an activation link, shown once
  And   I never see or set a password

Scenario: Reject an email already in use
  Given an account exists with email "ana@example.com"
  When  I create another with that email
  Then  it is not created
  And   I see "Ya existe una cuenta con este correo."

Scenario: Generate a new activation link
  Given the account of Ana is pending
  When  I generate a new link
  Then  the earlier link stops working

Scenario: List the accounts
  Given I am signed in with the role ADMIN
  When  I open "Usuarios"
  Then  I see each account's name, email, role and state, ordered by email
  And   no response field carries a password or its hash

Scenario: Another role is refused
  Given I am signed in with the role USER
  When  I call GET /users or POST /users
  Then  the API answers 403 with the error FORBIDDEN
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `identity` |
| Applicable NFR(s) | `NFR-05`, `NFR-06` |
| Business rule(s) touched | `BR-07` |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`identity.yaml`](../../07-api/contracts/openapi/identity.yaml) — `GET` and `POST /users`, `POST /users/{userId}/activation-links` |
| Screen | [`users.md`](../../12-ux-ui/screens/users.md) |
| Test cases | `TC-001`, `TC-020`, `TC-022` |
| Blocked by | `HU-01` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`ADR-009`](../../05-architecture/decisions/records/ADR-009-one-business-with-a-shared-catalog.md)
