# 00 — Security Policy

This project ships one application whose source lives in five repositories
(`bud-tack-docs`, `-api`, `-portal`, `-db`, `-infra`). Its controls apply to one running application — one
backend process, one database — and each code repository applies them on its own.

## Owner

The tech lead owns this policy: approves every waiver, receives every incident
report, and confirms that a rotation happened.

## Secrets management

| Rule | Detail |
|---|---|
| No secret in source control | Enforced in every repository by a pre-commit hook and a CI secret scanner |
| Local development | Copy the `.env.example` at the root of the repository you are running and fill it with local-only values; this repository's own is [`../.env.example`](../.env.example) |
| Staging and production | Injected by the deployment platform's secret store, never a committed file |
| Rotation | Every 90 days, or immediately after a suspected leak |

## Dependency scanning

- A CI job in each code repository scans every pull request for known-vulnerable
  dependencies before merge.
- A **critical** or **high** severity finding blocks the merge until it is fixed,
  or waived in writing by the tech lead with an expiry date on the waiver.
- Dependencies are upgraded on a monthly cadence even with no known vulnerability.

## License

The MIT `LICENSE` file, with its copyright notice, stays unchanged at the root
of every repository. A dependency is adopted only if its license allows
distribution inside an MIT-licensed application; the reviewer checks it in the
pull request that adds the dependency.

## Authentication and authorization

- **Authentication:** the `identity` module is the only place credentials are
  checked; a session or token is validated once, at the edge of the request,
  before any handler runs.
- **Password storage:** a password is stored only as a BCrypt hash (cost 12, over
  a SHA-384 digest of the password, so nothing past BCrypt's 72 bytes is ignored)
  and is never logged — **NFR-06**, [`ADR-008`](../05-architecture/decisions/records/ADR-008-bcrypt-cors-and-swagger.md).
- **Authorization:** two roles, `USER` and `ADMIN`
  ([`ADR-009`](../05-architecture/decisions/records/ADR-009-one-business-with-a-shared-catalog.md));
  a write endpoint that accepts an unauthenticated request is a bug — **NFR-05**.
  A role is checked at the edge of the request, by a named authorization policy
  on the endpoint. A `USER` only reaches transactions they recorded.
- **Account links:** activation and password reset use a random single-use token,
  and only its hash is stored
  ([`ADR-010`](../05-architecture/decisions/records/ADR-010-single-use-account-links.md)).
  No administrator sees or sets a password.
- **Least privilege:** the application's own database user cannot `DROP` or
  `ALTER` tables; migrations from `bud-tack-db` run under a separate, more
  privileged role.

## Incident response

A suspected breach is reported to the tech lead within one hour of discovery,
and any secret touched by the incident is rotated before the root cause is
confirmed.

---

**Related:** [`documentation-rules.md`](./documentation-rules.md) · [`README.md`](./README.md)
