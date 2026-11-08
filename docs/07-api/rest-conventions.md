# 07 — REST Conventions

## One API, not one per module

The application exposes exactly one HTTP API to its clients. A module's public
API (`TransactionsFacade`, `IdentityFacade`, `DashboardFacade`) is an in-process
boundary between modules, defined in
[`module-boundaries.md`](../02-domain/module-boundaries.md) — it is never called
over HTTP, and it does not get its own version number. Versioning below applies
once, to the whole application.

| Element | Convention | Example |
|---|---|---|
| Base path | `/api/v{n}` | `/api/v1` |
| Version bump | A breaking change to any endpoint's request or response bumps `{n}` for the whole API | `/api/v1` → `/api/v2` |

## Resource naming

| Rule | Example |
|---|---|
| Plural nouns, not verbs | `/api/v1/transactions`, not `/api/v1/getTransactions` |
| Deactivation is a `PATCH` of `isActive`, never a `DELETE` for a row in use | `PATCH /api/v1/categories/{categoryId}` with `{ "isActive": false }` (BR-05) |
| Nesting shows ownership | `/api/v1/categories/{categoryId}/subcategories` — a subcategory belongs to one category |
| kebab-case in multi-word paths | `/api/v1/transaction-details` |

## Status codes

| Code | When |
|---|---|
| 200 / 201 / 204 | Success reading, creating, or with no response body; a 201 carries a `Location` header |
| 400 | The request is malformed: wrong type, missing field |
| 401 | No valid credentials, or sign-in with wrong ones |
| 403 | Valid credentials, but the role may not call this endpoint — e.g. a `USER` calling `GET /users` |
| 404 | The resource, or its parent in a nested path, does not exist — including a soft-deleted transaction (**BR-04**) |
| 409 | The request conflicts with the resource's current state — e.g. a duplicate subcategory name, or deleting a category that still owns subcategories (**BR-05**) |
| 422 | The request is well-formed but fails a business rule — e.g. a transaction mixing income and expense details (**BR-02**), or a budget whose period ends before it starts |
| 500 | The application failed to handle the request at all — an unhandled error, logged with its `correlationId` per **NFR-09** |

`403` is about the role, never about the data: an authenticated user asking for
another business's data gets `404`.

## Error format

```json
{ "error": "BUSINESS_RULE_VIOLATED", "message": "a transaction cannot mix income and expense details",
  "details": [{ "field": "details[1].subcategoryId", "message": "is an expense subcategory" }] }
```

Every non-2xx response uses this shape. `error` is a stable machine code —
`VALIDATION_ERROR`, `BUSINESS_RULE_VIOLATED`, `CONFLICT`, `NOT_FOUND`,
`UNAUTHORIZED`, `FORBIDDEN`, `INTERNAL_ERROR` — and never changes once published; `message`
is for humans; `details` is present only for 400 and 422.

## Authentication

Sign-up (`POST /users`) and sign-in (`POST /sessions`) are the only write
endpoints that accept an unauthenticated request. Every other write endpoint
rejects one, per **NFR-05**. Swagger UI at `/swagger-ui.html` is public and calls
nothing by itself; its "Authorize" button takes a token from `POST /sessions`.

## Pagination

List endpoints paginate with `?page=1&limit=20` (`limit` capped at 100). The
dashboard's five most recent transactions are a fixed-size read, not a list, and
take no paging parameters. The default keeps `transactions` list endpoints inside
the p95 budget set by **NFR-01**, without a second, competing number here.

---

**Related:** [`../06-data/models.md`](../06-data/models.md) · [`./contracts/openapi/_template-api.yaml`](./contracts/openapi/_template-api.yaml)
