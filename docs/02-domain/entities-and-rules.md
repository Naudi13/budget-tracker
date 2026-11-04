# 02 — Entities and Rules

## Entities

| Entity | Key attributes | Invariants |
|---|---|---|
| User | name, email, password_hash | Email is unique; V1 has a single account per business, with no differentiated roles |
| Category | name, type (income \| expense) | Name is unique within its own type |
| Subcategory | category reference, name | Name is unique within its category |
| Transaction | transaction date, description | Deleting a transaction is a soft delete, never a physical removal |
| Transaction detail | transaction reference, subcategory reference, amount | Amount is always strictly greater than zero |
| Budget | category reference, amount, period start, period end | Amount is greater than zero; period end is always after period start |

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
   totals but never removes it from storage (soft delete). Traced from `HU-05`, `HU-06`.
5. **BR-05** — A category cannot be deleted while it owns a subcategory or a
   budget; a subcategory cannot be deleted while a transaction detail
   references it. Both are deactivated instead. Traced from `HU-03`.
6. **BR-06** — A budget can be changed or deleted at any time. Deleting a
   budget removes it from storage: it is a plan, not a record of money moved,
   so the soft delete of **BR-04** does not apply. A changed budget still
   satisfies the Budget invariants above. Traced from `HU-07`.

## Where a rule is enforced

Every `BR-NN` rule lives in the module that owns the entity it governs — see
[`module-boundaries.md`](./module-boundaries.md) for which module that is. A
rule enforced in two modules is a sign the boundary is in the wrong place.

---

**Related:** [`./domain-map.md`](./domain-map.md) · [`./module-boundaries.md`](./module-boundaries.md)
