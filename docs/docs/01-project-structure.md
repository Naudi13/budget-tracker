# 01 — Project Structure

## Standard Folder Layout

```
src/main/java/com/<company>/<app>/
+-- controller/    HTTP layer — one class per resource
+-- service/       Business logic — interfaces + implementations
+-- repository/    Data access — Spring Data repositories
+-- domain/        Entities, value objects, enums
+-- config/        Spring configuration classes
+-- dto/           Request/response objects (Java records)
+-- exception/     Custom exceptions and global handlers
src/main/resources/
+-- application.yml
+-- db/migration/  Liquibase changesets
src/test/java/...  Mirrors main structure
```

## Rules

- One class per file, filename matches class name exactly.
- No `@Bean` methods in controllers or services — use `config/` only.
- DTOs are immutable: use Java records (Java 17+) or final classes.
