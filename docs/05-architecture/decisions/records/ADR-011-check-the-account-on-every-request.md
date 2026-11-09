# ADR-011 — Every Request Checks That Its Account Is Active

## Identification

| Field | Value |
|---|---|
| Id | ADR-011 |
| Date | 2026-11-09 |
| Status | Accepted |
| Authors | Tech lead and team |
| Supersedes | none |

## Context

An administrator can now inactivate an account. The bearer token is checked without
reading the database, so an inactivated person would keep working until the token
expired, up to `AUTH_JWT_EXPIRY_SECONDS`.

| Constraint | Source |
|---|---|
| Inactivating an account ends its access | `HU-13`, `BR-09` |
| `identity` is the only module that checks credentials | [`security-policy.md`](../../../00-governance/security-policy.md) |

## Decision

**We decided:** after validating the token, `identity` reads whether its account is
still active, and refuses the request with `401` when it is not.

The tiebreaker was the meaning of the action: an administrator who turns an account
off expects it to be off now.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| Read the account on every request | Takes effect at once; one indexed read | One more query per request | Chosen |
| Wait for the token to expire | No query | Access continues up to an hour | Does not end access |
| A list of revoked tokens | Fewer reads | A new table and its cleanup, for the same read | More work, no gain here |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | Inactivating takes effect on the next request |
| Cost | One primary-key read per authenticated request |
| Risk | The read adds latency; the signal is `NFR-01`'s histogram |

**Documents to update:** [`security-policy.md`](../../../00-governance/security-policy.md),
[`identity.yaml`](../../../07-api/contracts/openapi/identity.yaml).

## Revisiting

Reopened if that read shows up as a meaningful share of request time.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-010-single-use-account-links.md`](./ADR-010-single-use-account-links.md)
