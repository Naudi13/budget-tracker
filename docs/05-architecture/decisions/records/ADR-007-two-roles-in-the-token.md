# ADR-007 — Two Roles, Carried in the Token

## Identification

| Field | Value |
|---|---|
| Id | ADR-007 |
| Date | 2026-11-08 |
| Status | Accepted |
| Authors | Tech lead and team |
| Supersedes | none |

## Context

V1 had one account per business and no roles, so no endpoint could answer `403`.
The course's project guide requires at least two roles with different permissions
on some endpoint, shown live as `401` without a token and `403` with the wrong
role. The change has to be small: the application is presented on 2026-11-09, and
one account must still never see another's data.

| Constraint | Source |
|---|---|
| At least two roles with different permissions | Course project guide, section 2 |
| One business never sees another's data | [`models.md`](../../../06-data/models.md) — `owned_by` |
| `identity` is the only module that checks credentials | [`security-policy.md`](../../../00-governance/security-policy.md) |

## Decision

**We decided:** `identity.users` has a `role` of `USER` or `ADMIN`, the token
carries it in a `role` claim, and a named authorization policy on the endpoint
checks it; `GET /users` is the one endpoint that requires `ADMIN`.

The tiebreaker was data isolation: listing accounts is a new capability that
reads only `identity.users`, so `owned_by` stays untouched everywhere else.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| `ADMIN` gains one new endpoint, `GET /users` | No existing rule changes; one clear `403` | `ADMIN` does little else | Chosen |
| `ADMIN` manages categories and budgets, `USER` only records transactions | Roles feel more distinct | A single-account business would need two accounts sharing data, which V1 does not model | Would break `owned_by` |
| `ADMIN` reads every account's transactions | Powerful | Removes the isolation the product promises | Rejected |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | Every endpoint that exists today keeps its behaviour and its tests |
| Positive | The check needs no database read: the role comes with the validated token |
| Cost | A migration adds `role`, and seed users exist for each role (`bud-tack-db`) |
| Risk | A role change only takes effect with the next token, up to `AUTH_JWT_EXPIRY_SECONDS` later |
| Risk | The seeded users have published test passwords; a real deployment must replace them before opening to anyone |

**Documents to update:** [`scope.md`](../../../01-context/scope.md),
[`security-policy.md`](../../../00-governance/security-policy.md),
[`rest-conventions.md`](../../../07-api/rest-conventions.md),
[`identity.yaml`](../../../07-api/contracts/openapi/identity.yaml),
[`models.md`](../../../06-data/models.md).

## Revisiting

Reopened when a business needs more than one person working on the same data.

---

**Related:** [`../README.md`](../README.md) · [`HU-09`](../../../04-requirements/stories/HU-09-list-accounts-as-admin.md)
