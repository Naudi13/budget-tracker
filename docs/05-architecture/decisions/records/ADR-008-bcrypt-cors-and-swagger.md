# ADR-008 — BCrypt, a CORS Allow-List and Swagger

## Identification

| Field | Value |
|---|---|
| Id | ADR-008 |
| Date | 2026-11-08 |
| Status | Accepted |
| Authors | Tech lead and team |
| Supersedes | The statement in [`ADR-006`](./ADR-006-portal-image-behind-reverse-proxy.md) that the API sends no CORS headers; the rest of ADR-006 stands |

## Context

The course's project guide asks for passwords stored with BCrypt, CORS limited to
the portal's origin, and Swagger at `/swagger-ui.html`. The API used PBKDF2, sent
no CORS headers and published no interactive documentation. ADR-002's stack does
not change: the guide's Spring Boot items map to their ASP.NET Core equivalents,
listed in the [README](../../../README.md).

| Constraint | Source |
|---|---|
| BCrypt, CORS for the portal only, Swagger | Course project guide, section 5 |
| Passwords of up to 128 characters | [`sign-up.md`](../../../12-ux-ui/screens/sign-up.md) |
| The browser reaches one origin, through the portal | [`ADR-006`](./ADR-006-portal-image-behind-reverse-proxy.md) |

## Decision

**We decided:** passwords are hashed with BCrypt at cost 12 over a SHA-384 digest;
the API allows exactly one cross-origin caller, `CORS_ALLOWED_ORIGIN`; and Swagger
UI is served anonymously at `/swagger-ui.html`, reachable through the portal.

The digest was the tiebreaker on hashing: plain BCrypt ignores everything past
72 bytes, and the sign-up form accepts 128 characters.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| BCrypt over a SHA-384 digest | Meets the guide; the whole password counts | Hashes come from the same library mode | Chosen |
| Plain BCrypt | Simplest | Long passwords silently truncated | Weaker for the passwords the form allows |
| Keep PBKDF2 | Already built and sound | Does not meet the guide | Rejected |
| Keep sending no CORS headers | Smallest surface | Does not meet the guide | Rejected |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | A request from any origin other than the portal's gets no CORS permission |
| Cost | Hashes already stored with PBKDF2 stop verifying; existing local databases are recreated |
| Cost | `bud-tack-portal`'s nginx also forwards `/swagger-ui.html` and `/swagger/` |
| Risk | Swagger is public; a real deployment should disable it, the signal being `/swagger-ui.html` answering `200` in production |

**Documents to update:** [`security-policy.md`](../../../00-governance/security-policy.md),
[`environments.md`](../../../10-devops/environments.md),
[`local-setup.md`](../../../10-devops/local-setup.md), [`.env.example`](../../../.env.example).

## Revisiting

Reopened if the portal must be served from more than one origin.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-007-two-roles-in-the-token.md`](./ADR-007-two-roles-in-the-token.md)
