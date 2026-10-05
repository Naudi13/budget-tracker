# Dashboard — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Dashboard |
| Serves | `HU-08` — see `../../04-requirements/user-stories.md` |
| Wireframe | Flow map "Dashboard" in `Budget-Tracker-Diagramas.pdf`, page 2 (not yet exported to the repository) |

The dashboard is the entry point: it links to *New record*, *Transaction history*
and *Configuration*. It has no input fields, so it has no validation rules.

## States

| State | What the user sees |
|---|---|
| Empty | Three KPI tiles at zero, and in the recent list the empty state "No transactions yet" with one action, *Record a transaction*, that opens the new record screen |
| Loading | KPI tiles and the recent list show skeleton blocks until the single dashboard request answers |
| Error | "The dashboard could not be loaded" with a *Retry* button; no partial figures are shown |
| Populated | Three KPI tiles — total income, total expenses, net balance (BR-03) — and the five most recent transactions, newest first |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| — | No input fields on this screen | — |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../04-requirements/user-stories.md`](../../04-requirements/user-stories.md)
