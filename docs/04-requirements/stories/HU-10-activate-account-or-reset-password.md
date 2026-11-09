# HU-10 — Activate an Account or Reset Its Password

## Story

**As** a person with an account
**I want** to choose my own password with a link from my administrator
**so that** I can get in, or back in, without anyone else knowing my password

## Acceptance criteria

```gherkin
Scenario: Activate an account
  Given I received a valid activation link
  When  I write a password of 8 or more characters, repeat it and save
  Then  my account is active and I am taken to sign-in

Scenario: Reset a forgotten password
  Given my account is active and an ADMIN generated a reset link
  When  I write a new password with that link
  Then  my new password works and the old one does not

Scenario: An administrator never knows the password
  Given I am signed in with the role ADMIN
  When  I generate a reset link for Ana
  Then  I get only the link, shown once

Scenario: A link works once
  Given I already used my link
  When  I use it again
  Then  I see "Este enlace no es válido o ya venció. Pide uno nuevo a tu administrador."

Scenario: A link expires
  Given my reset link was generated more than 30 minutes ago
  When  I use it
  Then  I see the same message

Scenario: Sign-in says where to go
  Given I forgot my password
  When  I open the sign-in screen
  Then  I see "¿Olvidaste tu contraseña? Pide a tu administrador un enlace para restablecerla."
```

## Affected module and requirements

| Field | Value |
|---|---|
| Module | `identity` |
| Applicable NFR(s) | `NFR-05`, `NFR-06` |
| Business rule(s) touched | `BR-07` |
| Repositories | `bud-tack-docs`, `-db`, `-api`, `-portal` |
| Contract | [`identity.yaml`](../../07-api/contracts/openapi/identity.yaml) — `POST /users/{userId}/password-resets`, `POST /account-links/confirm` |
| Screen | [`activate-account.md`](../../12-ux-ui/screens/activate-account.md) |
| Test case | `TC-023` |
| Blocked by | `HU-09` |

## Definition of ready

This story does not enter sprint planning until it meets
[`definition-of-ready.md`](../../00-governance/definition-of-ready.md) in full.

---

**Related:** [`../user-stories.md`](../user-stories.md) · [`ADR-010`](../../05-architecture/decisions/records/ADR-010-single-use-account-links.md)
