# TC-024 — Only an Administrator Manages the Catalog

## Identification

| Field | Value |
|---|---|
| Id | TC-024 |
| Covers | `HU-12` |
| Layer | `end-to-end` |
| Module | `transactions` |

## Preconditions

- One `ADMIN` and one `USER`, each signed in, and a category with a budget.

## Steps

1. As `USER`, call every write on categories, subcategories and budgets, and `GET /budgets`.
2. As `USER`, `GET /categories` and its subcategories.
3. As `ADMIN`, repeat step 1.

## Expected result

1. `403 FORBIDDEN`; nothing changes.
2. `200`.
3. Each succeeds.

---

**Related:** [`../testing-strategy.md`](../testing-strategy.md)
