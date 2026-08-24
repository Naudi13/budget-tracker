# 02 — Layered Architecture

## Layers

| Layer | Package | Responsibility | Allowed Dependencies |
|-------|---------|----------------|----------------------|
| Controller | `controller/` | HTTP: parse, delegate, respond | Service, DTO |
| Service | `service/` | Business logic, transactions | Repository, Domain |
| Repository | `repository/` | Data access only | Domain |
| Domain | `domain/` | Entities, value objects | None |

## Dependency Direction

```
Controller -> Service -> Repository -> Domain
```

Domain has zero outward dependencies. Controllers never call repositories directly.
Services never import Spring MVC classes.
