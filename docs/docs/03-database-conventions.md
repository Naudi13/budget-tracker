# 03 — Database Conventions

## Table Naming
- snake_case, plural: `users`, `order_items`
- Junction tables: `user_roles`, `product_categories`

## Column Naming
- snake_case: `first_name`, `created_at`, `is_active`
- Primary key: always `id` (BIGSERIAL or UUID)
- Foreign keys: `<table_singular>_id` — e.g. `user_id`
- Audit (required on every table): `created_at TIMESTAMP NOT NULL DEFAULT NOW()`, `updated_at TIMESTAMP NOT NULL DEFAULT NOW()`
- Booleans: prefix `is_` or `has_`

## Migrations
- Use Liquibase — see [liquibase-migration-standards](https://github.com/jesusarielgb-works/liquibase-migration-standards)
- One changeset per logical change — never modify an existing changeset

## JPA Rules
- FetchType: LAZY on all collections by default
- Avoid bidirectional relationships unless required
