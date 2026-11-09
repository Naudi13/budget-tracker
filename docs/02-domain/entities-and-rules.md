# 02 — Entities and Rules

## Entities

| Entity | Key attributes | Invariants |
|---|---|---|
| User | name, email, password_hash (none until activated), role, is_active | Email is unique; role is `USER` or `ADMIN`; an account is `PENDING` until its owner sets a password, then `ACTIVE`, and `INACTIVE` whenever an administrator turns it off |
| Category | name, type (income \| expense) | Name is unique within its type, ignoring case |
| Subcategory | category reference, name | Name is unique within its category, ignoring case |
| Transaction | owner, transaction date, description | Deleting a transaction is a soft delete with a reason, never a physical removal |
| Transaction detail | transaction reference, subcategory reference, amount | Amount is always strictly greater than zero |
| Budget | category reference, amount, period start, period end | Amount is greater than zero; period end is always after period start |

One installation serves one business. Categories, subcategories and budgets are
the business's, and only an `ADMIN` changes them. Each transaction belongs to the
user who recorded it: a `USER` reads and deletes only their own, and asking for
anyone else's answers `404`, as if it did not exist
(`07-api/rest-conventions.md`). An `ADMIN` reaches every transaction.

## Business rules

No ADR governs these rules yet — each is traced to the user story in
[`../04-requirements/user-stories.md`](../04-requirements/user-stories.md)
that first required it, until an ADR supersedes that trace.

1. **BR-01** — A transaction detail's type (income or expense) is derived
   from its subcategory's category and is never stored directly on the detail.
   Traced from `HU-04`.
2. **BR-02** — All details within one transaction share the same type; a
   transaction cannot mix income and expense details in V1. Traced from `HU-04`.
3. **BR-03** — The net balance is computed as the sum of income transaction
   details minus the sum of expense transaction details; it is never
   persisted, only calculated on read. Traced from `HU-08`.
4. **BR-04** — Deleting a transaction hides it from listings and dashboard
   totals but never removes it from storage (soft delete), and always records
   who deleted it, when and why. Traced from `HU-05`, `HU-06`.
5. **BR-05** — A category cannot be deleted while it owns a subcategory or a
   budget; a subcategory cannot be deleted while a transaction detail
   references it. Both are deactivated instead. Traced from `HU-03`.
6. **BR-06** — A budget can be changed or deleted at any time. Deleting a
   budget removes it from storage: it is a plan, not a record of money moved,
   so the soft delete of **BR-04** does not apply. A changed budget still
   satisfies the Budget invariants above. Traced from `HU-07`.
7. **BR-07** — Nobody signs up: an `ADMIN` creates each account. Its owner sets
   the password with a single-use link, and no administrator ever sees or sets a
   password. Traced from `HU-09`, `HU-10`.
8. **BR-08** — A budget counts while today is inside its period. Spent is the sum
   of the expense details of its category dated inside that period, from every
   account, computed on read. Available is the budget minus spent. Globally, only
   budgets of expense categories count. Traced from `HU-11`.
9. **BR-09** — An inactive account cannot sign in, and any session it had stops
   working at once. An administrator cannot inactivate their own account, so the
   application always keeps an active administrator. Only an account that was
   never activated can be deleted; one with a password is inactivated, because
   its transactions keep naming who recorded them. Traced from `HU-13`.

## Where a rule is enforced

Every `BR-NN` rule lives in the module that owns the entity it governs — see
[`module-boundaries.md`](./module-boundaries.md) for which module that is. A
rule enforced in two modules is a sign the boundary is in the wrong place.

---

**Related:** [`./domain-map.md`](./domain-map.md) · [`./module-boundaries.md`](./module-boundaries.md)
