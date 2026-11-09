# ADR-009 — One Business per Installation, with a Shared Catalog

## Identification

| Field | Value |
|---|---|
| Id | ADR-009 |
| Date | 2026-11-09 |
| Status | Accepted |
| Authors | Tech lead and team |
| Supersedes | The parts of [`ADR-007`](./ADR-007-two-roles-in-the-token.md) that limit `ADMIN` to `GET /users` and keep every row under `owned_by`; the role in the token stands |

## Context

At the presentation of 2026-11-09 the product changed: an administrator creates
the accounts and manages categories, subcategories and budgets, and a user only
records transactions and reads the dashboard. Until now each account owned its own
categories, so a `USER` could never pick one an administrator created.

| Constraint | Source |
|---|---|
| Only `ADMIN` manages categories, subcategories and budgets | Requirements of the 2026-11-09 review |
| A `USER` sees only their own transactions; `ADMIN` sees them all | Requirements of the 2026-11-09 review |
| Budgets are visible on every user's dashboard | Requirements of the 2026-11-09 review |

## Decision

**We decided:** one installation serves one business; categories, subcategories
and budgets have no owner and only `ADMIN` writes them, while each transaction
keeps its `owned_by` and a `USER` reaches only their own.

The tiebreaker was the transaction form: a `USER` needs the administrator's
categories to record anything, so the catalog cannot belong to one account.
A `USER` can read categories through the API, for that form and the dashboard
filter, and has no screen to manage them.

## Alternatives evaluated

| Alternative | Pros | Cons | Verdict |
|---|---|---|---|
| One business, shared catalog, transactions per user | Matches every requirement; small migration | One business per installation | Chosen |
| A business entity with users and data under it | Several businesses in one installation | A new table and key across every query | Nothing asks for several businesses |
| Keep `owned_by` everywhere and copy categories to each user | No schema change | Copies drift; budgets stop being the business's | Rejected |

## Consequences

| Kind | Consequence |
|---|---|
| Positive | The catalog and budgets are managed in one place |
| Cost | A migration removes `owned_by` from categories and budgets; category names become unique per type |
| Cost | Tests, Postman and the demo script stop signing up and act as `ADMIN` for the catalog |
| Risk | The dashboard shows a `USER` the business's total against the budget, not other people's transactions; the signal is a request to hide it |

**Documents to update:** [`scope.md`](../../../01-context/scope.md),
[`entities-and-rules.md`](../../../02-domain/entities-and-rules.md),
[`models.md`](../../../06-data/models.md),
[`rest-conventions.md`](../../../07-api/rest-conventions.md).

## Revisiting

Reopened when one installation has to serve more than one business.

---

**Related:** [`../README.md`](../README.md) · [`./ADR-010-single-use-account-links.md`](./ADR-010-single-use-account-links.md)
