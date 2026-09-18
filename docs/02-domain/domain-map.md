# 02 — Domain Map

## Bounded contexts

```mermaid
graph TD
    Transactions["Transactions<br/>transactions, categories"]
    Dashboard["Dashboard<br/>KPIs, recent transactions"]

    Dashboard -->|reads transactions from| Transactions
```

## Relationship legend

| Arrow | Meaning |
|---|---|
| `reads transactions from` | Read-only; Dashboard owns no transaction data. Total income, total expenses, and net balance are computed on read from Transactions per **BR-03**, and the 5 most recent transactions are read the same way, unaggregated |

## How this map is used

Every bounded context above becomes exactly one module in
[`module-boundaries.md`](./module-boundaries.md), owning the tables named here.
A context with no box on this map has no reason to be its own module either.

---

**Related:** [`./entities-and-rules.md`](./entities-and-rules.md) · [`./module-boundaries.md`](./module-boundaries.md)
