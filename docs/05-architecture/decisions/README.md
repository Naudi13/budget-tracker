# 05 — Architecture Decision Records

> [!NOTE] INSTRUCTIONS
> An ADR records one decision that is expensive to reverse: what was decided,
> what else was considered, and what it costs. Copy `_template-adr.md` into
> `records/`, never edit the template in place, and add a row to the register
> below in the same pull request. Delete this block once the register has a
> second row that this team wrote itself.

## When an ADR is written

| Write one when | Do not write one when |
|---|---|
| The decision changes the shape of the code — layers, modules, boundaries | The choice is reversible in an afternoon |
| Reversing it later means moving files across boundaries | It only affects one file |
| A reviewer asked "why did we do it this way?" twice | The answer is already in a document in this repository |
| A boundary suppression is being introduced | A style question a linter can settle |
| A dependency is adopted that the whole application will import | A library used inside one module only |

A decision taken in a meeting and not written down has not been made; it has
only been discussed out loud.

## Statuses

| Status | Meaning | Next step |
|---|---|---|
| `Proposed` | Written, under review, not yet binding | Review at the architecture gate |
| `Accepted` | Binding on the codebase from its date onward | None — it is now the rule |
| `Rejected` | Considered and turned down; kept so it is not re-proposed | None |
| `Superseded by ADR-NNN` | Replaced by a later decision | Read the ADR that replaced it |

An accepted ADR is never edited or deleted. It is superseded by a new record
that names it, so the history of the reasoning stays readable.

## Register

| # | Title | Status | Date |
|---|---|---|---|
| [ADR-001](./records/ADR-001-modular-monolith-as-default.md) | Modular monolith as the default architecture | Accepted | 2026-09-12 |

## Naming

`ADR-NNN-short-title.md` — three digits, sequential from `001`, never reused,
in [`records/`](./records/ADR-001-modular-monolith-as-default.md). The number is
assigned when the record is written, not when it is accepted.

---

**Related:** [`./_template-adr.md`](./_template-adr.md) · [`../README.md`](../README.md)
