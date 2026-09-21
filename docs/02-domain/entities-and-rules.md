# 02 — Entities and Rules

## Entities

| Entity | Key attributes | Invariants |
|---|---|---|
| Transaction | amount, type (income \| expense), category, date, description | Amount is always strictly greater than zero; whether it adds to or subtracts from the balance is decided by type, never by a negative amount |
| Category | name, type (income \| expense) | Name is unique within its own type |

## Business rules

1. **BR-01** — A transaction's amount must be strictly greater than zero.
2. **BR-02** — A transaction must reference a category of the same type; an
   expense transaction cannot use an income category, and vice versa.
3. **BR-03** — The net balance is computed as the sum of income transactions
   minus the sum of expense transactions; it is never persisted, only
   calculated on read.
4. **BR-04** — A category referenced by at least one transaction cannot be
   deleted, only deactivated.

## Where a rule is enforced

Every `BR-NN` rule lives in the module that owns the entity it governs — see
[`module-boundaries.md`](./module-boundaries.md) for which module that is. A
rule enforced in two modules is a sign the boundary is in the wrong place.

---

**Related:** [`./domain-map.md`](./domain-map.md) · [`./module-boundaries.md`](./module-boundaries.md)
