# Transaction History — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Transaction history |
| Serves | `HU-05` (view) and `HU-06` (delete) — see `../../04-requirements/user-stories.md` |
| Wireframe | Flow "Ver historial" and "Eliminar transacción" in `Budget-Tracker-Diagramas.pdf`, pages 3–4 (not yet exported to the repository) |

Deleting is a soft delete (BR-04): the row disappears from this list and from
the dashboard totals, but is kept in storage.

## States

| State | What the user sees |
|---|---|
| Empty | "No transactions recorded yet" with one action, *Record a transaction* |
| Loading | Table skeleton while the current page is requested |
| Error | "The history could not be loaded." with *Retry*; the previous page is not shown as current |
| Populated | A page of transactions, newest first, with date, description, type, total amount and a *Delete* action per row |

## Delete flow

| Step | What the user sees |
|---|---|
| 1. Select *Delete* on a row | A confirm dialog: "Delete this transaction? It will be removed from the history and from the dashboard totals." |
| 2a. *Cancel* | The dialog closes; the list is unchanged |
| 2b. *Delete* (danger) | The row leaves the list; a success banner "Transaction deleted." appears; the dashboard recomputes on return |
| 2c. Request fails | The dialog stays open with "The transaction was not deleted. Try again." |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Page size | Fixed by the list request; no user input | — |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../04-requirements/user-stories.md`](../../04-requirements/user-stories.md)
