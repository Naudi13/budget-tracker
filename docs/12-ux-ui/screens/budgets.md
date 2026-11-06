# Budgets — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Configuration › Budgets |
| Serves | [`HU-07`](../../04-requirements/stories/HU-07-set-budget.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌──────────────────────────────────────────────────────────┐
│ Presupuestos                    [ Nuevo presupuesto ]    │  ← primary button
│ Categoría [ Todas ▾ ]                                    │  ← filter, ?categoryId=
│ ┌───────────┬────────────┬────────────┬────────────┬───┐ │
│ │ Categoría │ Monto      │ Desde      │ Hasta      │   │ │
│ │ Oficina   │ $ 500.000  │ 01/11/2026 │ 30/11/2026 │✎🗑│ │  ← edit, delete
│ └───────────┴────────────┴────────────┴────────────┴───┘ │
│                                 ‹ 1 2 › (20 por página)  │
└──────────────────────────────────────────────────────────┘
```

*Nuevo presupuesto* opens a form with *Categoría*, *Monto*, *Desde* and *Hasta*;
*Editar* opens the same form filled in (`PATCH`). Saving returns to the list with
the banner "Presupuesto guardado." *Eliminar* opens the confirm dialog "¿Eliminar
el presupuesto de 'Oficina'? No se puede deshacer." with a danger *Eliminar* and a
*Cancelar* (BR-06).
V1 only stores the limit: no actual-versus-budget figure is shown
([`scope.md`](../../01-context/scope.md)).

## States

| State | What the user sees |
|---|---|
| Empty | "Aún no tienes presupuestos" with one action, *Nuevo presupuesto* |
| Loading | Table skeleton while budgets are requested |
| Error | "No pudimos cargar los presupuestos." with *Reintentar*; a failed save keeps the form filled |
| Populated | The paginated table, newest period first |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Category (*Categoría*) | Required; only active categories are offered (BR-05) | "Elige una categoría. Las inactivas no aparecen." |
| Amount (*Monto*) | Required, strictly greater than zero | "Escribe un monto mayor que cero." |
| From (*Desde*) | Required, a valid date | "Elige el primer día del periodo." |
| To (*Hasta*) | Required, after *Desde* (`422` from the API) | "La fecha final debe ser posterior a la inicial." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`./categories-configuration.md`](./categories-configuration.md)
