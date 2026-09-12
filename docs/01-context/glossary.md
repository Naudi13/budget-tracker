# 01 — Glossary

> [!NOTE] INSTRUCTIONS
> Add every term a newcomer would have to ask about. A term used differently in
> two documents is a bug in this glossary, not in those documents. Delete this
> block once every document in the repository has been checked against it.

## Terms

| Term | Definition | Forbidden synonyms |
|---|---|---|
| Product | An item the business sells, tracked in the catalog | Item, SKU (use only in database contexts) |
| Category | A named grouping of products used for browsing and reporting | Tag, label |
| Order | A customer's request for one or more products, before it is invoiced | Cart, ticket |
| Invoice | The billing document generated from an order, owed by a customer | Bill, receipt |
| Payment | A recorded amount applied against an invoice's balance | Transaction, charge |
| Module | A bounded context inside the single deployable, defined in [`../02-domain/module-boundaries.md`](../02-domain/module-boundaries.md) | Service, component |

## Why forbidden synonyms matter

Two words for the same thing let two documents describe the same rule
differently and both look correct. When a reviewer sees a forbidden synonym in
a pull request, the fix is to rename the usage, not to add the synonym here.

## Adding a term

A term is added here the first time it appears in a second document. A term
used in exactly one document belongs in that document, not here.

---

**Related:** [`./overview.md`](./overview.md) · [`../02-domain/module-boundaries.md`](../02-domain/module-boundaries.md)
