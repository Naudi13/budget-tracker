# ADR-006 — The Portal Ships as Its Own Image, Behind a Reverse Proxy

## Identification

| Field | Value |
|---|---|
| Id | ADR-006 |
| Date | 2026-11-04 |
| Status | Accepted |
| Authors | Tech lead and team, at the architecture review gate |
| Supersedes | none |

## Context

The application is one deployable backend ([`ADR-001`](./ADR-001-modular-monolith-as-default.md)),
but its Angular client lives in `bud-tack-portal` ([`ADR-004`](./ADR-004-shared-database-and-per-piece-repositories.md))
and compiles to static files. Nothing said how those files reach `staging` and
`production`, nor how the browser reaches the API without cross-origin requests.

| Constraint | Source |
|---|---|
| One backend process, one build of it, released as a unit | [`overview.md`](../../../01-context/overview.md) |
| Each repository has its own pipeline, blocking its own merges | [`ci-cd.md`](../../../10-devops/ci-cd.md) |
| Zero write endpoints accept an unauthenticated request | `NFR-05` |
| Only Angular, .NET and PostgreSQL as application toolchains | [`ADR-005`](./ADR-005-fluentmigrator-for-schema-migrations.md) |

## Decision

**We decided:** `bud-tack-portal` packages its `ng build` output in its own
container image, an nginx that serves the files and forwards `/api/` and
`/health` to the API; `bud-tack-infra` runs both images side by side, pinned to
the same version tag, so the browser only ever talks to one origin.

The tiebreaker was independence: each repository builds and proves its own image,
and the API's pipeline never needs a second repository checked out.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| Portal image (nginx) with a reverse proxy to the API | Independent pipelines; one origin, so no CORS; static files cached by nginx | Two images to keep on the same tag | Chosen |
| Portal built into the API's `wwwroot` | Literally one image | The API's `package` stage depends on `bud-tack-portal`; every portal change rebuilds the API | Couples two repositories' pipelines |
| Portal on its own domain, calling the API with CORS | Simplest images | A cross-origin API, with a CORS policy to keep right in every environment | Rejected: a wider surface for no gain |
| An ASP.NET Core host with YARP instead of nginx | .NET end to end | A second .NET service to write and maintain only to serve files | nginx is configuration, not application code |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | The API sends no CORS headers in any environment: every origin but its own is refused |
| Positive | A portal-only change ships without rebuilding or redeploying the API |
| Cost | `bud-tack-portal` owns a `Dockerfile` and an `nginx.conf`; `bud-tack-infra` owns the Compose file pinning both tags |
| Risk | The two images drift to different versions; the signal is a release whose Compose file names two different tags |

"The one artifact" in `ci-cd.md` keeps meaning the API's image: the one process
that holds the modules. The portal image is its client, never a second backend.

**Documents to update:** [`../../../10-devops/ci-cd.md`](../../../10-devops/ci-cd.md),
[`../../../10-devops/environments.md`](../../../10-devops/environments.md),
[`../../../10-devops/local-setup.md`](../../../10-devops/local-setup.md),
[`../../modular-monolith.md`](../../modular-monolith.md).

## Revisiting

Reopened if a release ships with mismatched portal and API tags twice in one
quarter, or if the portal needs server-side rendering, which nginx cannot do.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-004-shared-database-and-per-piece-repositories.md`](./ADR-004-shared-database-and-per-piece-repositories.md)
