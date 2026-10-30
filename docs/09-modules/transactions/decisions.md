# transactions — Decisions

## [DEC-01] Transactions follow a header-detail pattern, not one flat row

| Field | Value |
|---|---|
| Date | 2026-09-21 |
| Status | `Active` |

**Context:** A single money movement can classify different amounts under
different subcategories (for example, a receipt split between "Office
supplies" and "Software"), so one `amount` and one `subcategory_id` on the
transaction itself cannot represent it.

**Decision:** `transactions.transactions` holds only the date and
description; every classified amount lives in
`transactions.transaction_details`, one row per subcategory. BR-02 keeps all
of a transaction's details on one side (income or expense).

**Global ADR, if any:** none — internal to this module; the module's public
API already hides the two-table split behind one `RecordTransaction` call.

## Adding the next decision

Copy the `[DEC-01]` block above, renumbered `[DEC-02]`, and add it below this
heading — newest last, so the file reads in the order decisions were made.

---

**Related:** [`./README.md`](./README.md) · [`../../05-architecture/decisions/README.md`](../../05-architecture/decisions/README.md)
