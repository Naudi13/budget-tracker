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
| Loading | Save button disabled and labelled "Saving…" while the request is in flight |
| Error | Banner "The record could not be saved. Your entries are kept — try again." with *Retry*; the form is not cleared |
| Populated | The entered form, ready to save; a success banner appears on return to the dashboard |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Transaction date | Required, a valid date | "Choose the date the money moved." |
| Description | Required, at least one character | "Describe the transaction, for example 'Office supplies'." |
| Detail — subcategory | Required; only active subcategories are offered (BR-05) | "Choose a subcategory. Inactive subcategories are not listed." |
| Detail — amount | Required, strictly greater than zero (BR-01) | "Enter an amount above zero." |
| Transaction — type | All detail lines share one type; choosing a subcategory of the other type is refused (BR-02) | "This transaction already has income lines. Remove them to add an expense, or save them as a separate transaction." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`../../02-domain/entities-and-rules.md`](../../02-domain/entities-and-rules.md)
