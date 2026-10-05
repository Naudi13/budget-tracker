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
| Empty | "No categories yet" with one action, *New category* |
| Loading | List skeleton while categories are requested |
| Error | "Categories could not be loaded." with *Retry* |
| Populated | Categories grouped by type, each with its subcategories and an *Active* / *Inactive* label |

## Delete flow

The flow follows **BR-05**, which differs from the diagram in the flow document —
see the note below the table.

| Step | What the user sees |
|---|---|
| 1. Select *Delete* on a category or subcategory | Confirm dialog: "Delete *name*?" |
| 2a. *Cancel* | Back to the list; nothing changes |
| 2b. *Delete* — not in use | Removed; success banner "*name* deleted." |
| 2c. *Delete* — in use | Blocked notice: "*name* is in use — it has transactions, subcategories or budgets. Deactivate it instead." with *Deactivate* as the action |
| 2d. *Deactivate* | The row shows *Inactive*; it is no longer offered on the new record screen |

Note: the flow document asks only whether a category has *transactions*. BR-05
blocks a category that *owns a subcategory or a budget* even when none of them
has a transaction yet, because the database's `ON DELETE RESTRICT` would refuse
the delete anyway. The screen follows BR-05.

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Category name | Required; unique within its type | "Enter a name." / "An *income* category named *name* already exists." |
| Category type | Required: income or expense | "Choose income or expense." |
| Subcategory name | Required; unique within its category | "Enter a name." / "This category already has a subcategory named *name*." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
