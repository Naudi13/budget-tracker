# New Record — Screen

## Identification

| Field | Value |
|---|---|
| Screen | New record (income or expense) |
| Serves | `HU-04` — see `../../04-requirements/user-stories.md` |
| Wireframe | Flow "Nuevo registro" in `Budget-Tracker-Diagramas.pdf`, page 3 (not yet exported to the repository) |

Flow: the form is filled, validated, and on success the KPIs are recomputed and
the user returns to the dashboard. An invalid form stays on screen with the
entered values kept.

## States

| State | What the user sees |
|---|---|
| Empty | Date set to today, description blank, one detail line with an empty subcategory and amount |
| Loading | *Guardar* disabled and labelled "Guardando…" while the request is in flight |
| Error | Banner "No pudimos guardar el registro. Tus datos siguen aquí, inténtalo de nuevo." with *Reintentar*; the form is not cleared |
| Populated | The entered form, ready to save; a success banner appears on return to the dashboard |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Transaction date (*Fecha*) | Required, a valid date | "Elige la fecha en que se movió el dinero." |
| Description (*Descripción*) | Required, at least one character | "Describe el movimiento, por ejemplo 'Útiles de oficina'." |
| Detail — subcategory (*Subcategoría*) | Required; only active subcategories are offered (BR-05) | "Elige una subcategoría. Las inactivas no aparecen." |
| Detail — amount (*Monto*) | Required, strictly greater than zero (BR-01) | "Escribe un monto mayor que cero." |
| Transaction — type | All detail lines share one type; choosing a subcategory of the other type is refused (BR-02) | "Este movimiento ya tiene líneas de ingreso. Quítalas para agregar un gasto, o guárdalas como otro movimiento." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
