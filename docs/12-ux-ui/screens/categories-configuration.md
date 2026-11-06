# Categories Configuration — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Configuration › Categories (with subcategories) |
| Serves | `HU-02` (create) and `HU-03` (delete) — see `../../04-requirements/user-stories.md` |
| Wireframe | Flow "Configuración › Categorías" in `Budget-Tracker-Diagramas.pdf`, page 5 (not yet exported to the repository) |

## States

| State | What the user sees |
|---|---|
| Empty | "Aún no tienes categorías" with one action, *Nueva categoría* |
| Loading | List skeleton while categories are requested |
| Error | "No pudimos cargar las categorías." with *Reintentar* |
| Populated | Categories grouped by type, each with its subcategories and an *Activa* / *Inactiva* label |

## Delete flow

The flow follows **BR-05**, which differs from the diagram in the flow document —
see the note below the table.

| Step | What the user sees |
|---|---|
| 1. Select *Eliminar* on a category or subcategory | Confirm dialog: "¿Eliminar *nombre*?" |
| 2a. *Cancelar* | Back to the list; nothing changes |
| 2b. *Eliminar* — not in use | Removed; success banner "*nombre* eliminada." |
| 2c. *Eliminar* — in use | Blocked notice: "*nombre* está en uso: tiene movimientos, subcategorías o presupuestos. Mejor desactívala." with *Desactivar* as the action |
| 2d. *Desactivar* | The row shows *Inactiva*; it is no longer offered on the new record screen |

Note: the flow document asks only whether a category has *transactions*. BR-05
blocks a category that *owns a subcategory or a budget* even when none of them
has a transaction yet, because the database's `ON DELETE RESTRICT` would refuse
the delete anyway. The screen follows BR-05.

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Category name (*Nombre*) | Required; unique within its type | "Escribe un nombre." / "Ya existe una categoría de *ingreso* llamada *nombre*." |
| Category type (*Tipo*) | Required: income or expense | "Elige ingreso o gasto." |
| Subcategory name (*Nombre*) | Required; unique within its category | "Escribe un nombre." / "Esta categoría ya tiene una subcategoría llamada *nombre*." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
