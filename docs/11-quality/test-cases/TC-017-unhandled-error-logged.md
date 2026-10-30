# TC-017 — An Unhandled Error Is Logged with a Request Id and Timestamp

## Identification

| Field | Value |
|---|---|
| Id | TC-017 |
| Covers | `NFR-09` |
| Layer | `end-to-end` |
| Module | whole artifact |

## Preconditions

- A request crafted to trigger an unhandled exception in a test
  environment (for example, an injected fault in a downstream call).

## Steps

1. Send the request that triggers the unhandled exception.
2. Inspect the resulting log entry.

## Expected result

The log entry includes a request id and a timestamp, matching the
structured log fields in
[`observability.md`](../../13-operations/observability.md).

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md) · [`../../13-operations/observability.md`](../../13-operations/observability.md)
