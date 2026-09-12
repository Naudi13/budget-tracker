# 04 — Traceability Matrix

> [!NOTE] INSTRUCTIONS
> Every `HU-NN` must appear at least once below, pointing at the module that
> implements it, the document that designs it, and the test that would fail
> if the story broke. A story with no row here is a story nobody can verify.
> Delete this block once every row resolves to a real document and test.

## How to read this table

`Requirement -> Module -> Design document -> Test case`. The design-document
and test-case columns point ahead to `05-architecture`, `09-modules`, and
`11-quality` — sections not yet written. A forward link here is expected, not
a defect.

## Matrix

| HU-NN | Module | Design document | Test case |
|---|---|---|---|
| HU-01 | `catalog` | [`module-catalog.md`](../09-modules/module-catalog.md) | [`unit`](../11-quality/testing-strategy.md) — reject a negative price |
| HU-02 | `catalog` | [`module-catalog.md`](../09-modules/module-catalog.md) | [`module-integration`](../11-quality/testing-strategy.md) — a retired product is blocked from a new invoice (BR-01) |
| HU-03 | `catalog` | [`module-catalog.md`](../09-modules/module-catalog.md) | [`unit`](../11-quality/testing-strategy.md) — a category with active products cannot be deleted (BR-04) |
| HU-04 | `billing` | [`module-catalog.md`](../09-modules/module-catalog.md) | [`module-integration`](../11-quality/testing-strategy.md) — a later price change does not alter an issued invoice (BR-02) |
| HU-05 | `billing` | [`module-catalog.md`](../09-modules/module-catalog.md) | [`unit`](../11-quality/testing-strategy.md) — a payment is rejected once the invoice is `paid` (BR-03) |
| HU-06 | `billing` | [`module-catalog.md`](../09-modules/module-catalog.md) | [`end-to-end`](../11-quality/testing-strategy.md) — the summary total equals the sum of the day's issued invoices |

## Gaps

A gap is an `HU-NN` with no row, or a row whose design document or test case
still does not exist once `05-architecture`, `09-modules`, and `11-quality`
are filled in. Until then, every forward link above is a gap by definition —
recheck this matrix once those sections land.

---

**Related:** [`./user-stories.md`](./user-stories.md) · [`./non-functional.md`](./non-functional.md)
