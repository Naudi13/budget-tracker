# Dashboard — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Dashboard |
| Serves | `HU-08` and `HU-11` — see `../../04-requirements/user-stories.md` |
| Wireframe | Flow map "Dashboard" in `Budget-Tracker-Diagramas.pdf`, page 2 (not yet exported to the repository) |

The dashboard is the entry point: it links to *Nuevo registro*, *Historial*
and, for an `ADMIN`, *Configuración*.

A filter, *Categoría*, offers *Todas* (the default) and every active category; the
whole screen follows it. Below the three KPI tiles, a band shows *Presupuesto*,
*Gastado* and *Disponible* (BR-08). A `USER`'s tiles and list are their own; the
band is the business's for everyone.

## States

| State | What the user sees |
|---|---|
| Empty | Three KPI tiles at zero, and in the recent list the empty state "Aún no tienes movimientos" with one action, *Registrar un movimiento*, that opens the new record screen |
| Loading | KPI tiles and the recent list show skeleton blocks until the single dashboard request answers |
| Error | "No pudimos cargar el panel." with a *Reintentar* button; no partial figures are shown |
| Populated | Three KPI tiles — *Ingresos*, *Gastos* and *Balance* (total income, total expenses, net balance; BR-03) — and the five most recent transactions, newest first |
| No budget in force | The band says "Sin presupuesto vigente" |
| Over budget | *Disponible* is red and keeps its minus sign, with the text "Sobre el presupuesto" |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Category filter | One of the listed options | — |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../04-requirements/user-stories.md`](../../04-requirements/user-stories.md)
