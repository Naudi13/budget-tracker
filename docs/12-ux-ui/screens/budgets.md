# Budgets — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Configuration › Budgets |
| Serves | [`HU-07`](../../04-requirements/stories/HU-07-set-budget.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌──────────────────────────────────────────────────────────┐
│ Budgets                              [ New budget ]      │  ← primary button
│ Category [ All ▾ ]                                       │  ← filter, ?categoryId=
│ ┌──────────┬────────┬────────────┬────────────┬────────┐ │
│ │ Category │ Amount │ From       │ To         │        │ │
│ │ Office   │ 500.00 │ 2026-11-01 │ 2026-11-30 │ ✎  🗑  │ │  ← edit, delete
│ └──────────┴────────┴────────────┴────────────┴────────┘ │
│                                   ‹ 1 2 › (20 per page)  │
└──────────────────────────────────────────────────────────┘
```

*New budget* opens a form with *Category*, *Amount*, *From* and *To*; *Edit*
opens the same form filled in (`PATCH`). Saving returns to the list with the
banner "Budget saved." *Delete* opens the confirm dialog "Delete the budget for
'Office'? This cannot be undone." with a danger *Delete* and a *Cancel* (BR-06).
V1 only stores the limit: no actual-versus-budget figure is shown
([`scope.md`](../../01-context/scope.md)).

## States

| State | What the user sees |
|---|---|
| Empty | "No budgets yet" with one action, *New budget* |
| Loading | Table skeleton while budgets are requested |
| Error | "Budgets could not be loaded." with *Retry*; a failed save keeps the form filled |
| Populated | The paginated table, newest period first |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Category | Required; only active categories are offered (BR-05) | "Choose a category. Inactive categories are not listed." |
| Amount | Required, strictly greater than zero | "Enter an amount above zero." |
| From | Required, a valid date | "Choose the first day of the period." |
| To | Required, after *From* (`422` from the API) | "The end date must be after the start date." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`./categories-configuration.md`](./categories-configuration.md)
