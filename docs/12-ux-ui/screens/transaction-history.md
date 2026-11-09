# Transaction History — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Transaction history |
| Serves | `HU-05` (view) and `HU-06` (delete with a reason) — see `../../04-requirements/user-stories.md` |
| Wireframe | Flow "Ver historial" and "Eliminar transacción" in `Budget-Tracker-Diagramas.pdf`, pages 3–4 (not yet exported to the repository) |

Deleting is a soft delete (BR-04): the row disappears from this list and from
the dashboard totals, but is kept in storage with who deleted it, when and why.

A `USER` sees their own transactions. An `ADMIN` sees all of them with a *Registrado
por* column, and a filter *Activos* / *Eliminados*; *Eliminados* shows who deleted
each one, when and why, with no actions.

## States

| State | What the user sees |
|---|---|
| Empty | "Aún no has registrado movimientos" with one action, *Registrar un movimiento* |
| Loading | Table skeleton while the current page is requested |
| Error | "No pudimos cargar el historial." with *Reintentar*; the previous page is not shown as current |
| Populated | A page of transactions, newest first, with date, description, type, total amount and an *Eliminar* action per row |

## Delete flow

| Step | What the user sees |
|---|---|
| 1. Select *Eliminar* on a row | A dialog: "¿Eliminar este movimiento? Saldrá del historial y de los totales del panel.", with a required field *Razón* |
| 2a. *Cancelar* | The dialog closes; the list is unchanged |
| 2b. *Eliminar* (danger) | The row leaves the list; a success banner "Movimiento eliminado." appears; the dashboard recomputes on return |
| 2c. Request fails | The dialog stays open with "No pudimos eliminar el movimiento. Inténtalo de nuevo." |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Page size | Fixed by the list request; no user input | — |
| Reason (*Razón*) | Required, 3 to 200 characters | "Escribe la razón (mínimo 3 caracteres)." / over 200: "Usa máximo 200 caracteres." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../04-requirements/user-stories.md`](../../04-requirements/user-stories.md)
