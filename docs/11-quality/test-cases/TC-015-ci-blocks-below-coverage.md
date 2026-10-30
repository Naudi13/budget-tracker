# TC-015 — CI Blocks a Pull Request Below the Coverage Threshold

## Identification

| Field | Value |
|---|---|
| Id | TC-015 |
| Covers | `NFR-07` |
| Layer | other — CI coverage gate, outside the three code layers |
| Module | whole artifact |

## Preconditions

- A pull request that lowers unit test coverage on a changed
  `BudTack.Transactions` file below 90%.

## Steps

1. Open the pull request and let the `test` stage run.

## Expected result

The `test` stage fails and the pull request cannot merge, per
[`definition-of-done.md`](../../00-governance/definition-of-done.md).

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../10-devops/ci-cd.md`](../../10-devops/ci-cd.md)
