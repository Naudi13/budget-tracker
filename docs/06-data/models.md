# 06 — Data Models

> [!NOTE] INSTRUCTIONS
> One row per table that survives a restart, sourced from
> `02-domain/entities-and-rules.md` — do not invent a table with no entity
> behind it. Delete this block once the diagram matches the real schema.

## Entities and ownership

| Entity | Table | Owning module | Key attributes |
|---|---|---|---|
| Category | `category` | `catalog` | name, parent category (self-referencing, optional) |
| Product | `product` | `catalog` | sku, name, price, category, active |
| Invoice | `invoice` | `billing` | total, status, issued date |
| Invoice line | `invoice_line` | `billing` | invoice, product reference, quantity, unit price |
| Payment | `payment` | `billing` | invoice reference, amount, method, recorded date |

Invariants for each entity live in
[`entities-and-rules.md`](../02-domain/entities-and-rules.md); this table
only adds the table name and the module that owns it.

## Relationships

```mermaid
erDiagram
    CATEGORY ||--o{ PRODUCT : classifies
    PRODUCT ||--o{ INVOICE_LINE : "appears on"
    INVOICE ||--|{ INVOICE_LINE : contains
    INVOICE ||--o{ PAYMENT : "settled by"
    CATEGORY {
        uuid id PK
        string name
        uuid parent_id FK
    }
    PRODUCT {
        uuid id PK
        string sku
        numeric price
        uuid category_id FK
        boolean active
    }
    INVOICE_LINE {
        uuid id PK
        uuid invoice_id FK
        uuid product_id FK
        integer quantity
        numeric unit_price
    }
```

## Cross-module references

| Table | Column | Points to | Enforced by |
|---|---|---|---|
| `invoice_line` | `product_id` | `catalog.product` | `CatalogFacade`, not a database foreign key — see [`database-conventions.md`](./database-conventions.md) |

`invoice_line.unit_price` is copied from `catalog` at issue time through
`CatalogFacade`, per **BR-02** — it is never read by joining `product` live.

---

**Related:** [`../02-domain/entities-and-rules.md`](../02-domain/entities-and-rules.md) · [`./database-conventions.md`](./database-conventions.md)
