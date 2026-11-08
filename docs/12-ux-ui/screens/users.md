# Users — Screen

## Identification

| Field | Value |
|---|---|
| Screen | List the accounts (route `/settings/users`) |
| Serves | [`HU-09`](../../04-requirements/stories/HU-09-list-accounts-as-admin.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌───────────────────────────────────────────────┐
│  Usuarios                                     │
│  Nombre     Correo              Rol           │
│  Ana        ana@example.com     Usuario       │
│  Admin      admin@example.com   Administrador │
│       ‹ Anterior  Página 1 de 1  Siguiente ›  │
└───────────────────────────────────────────────┘
```

Only the role `ADMIN` sees the "Usuarios" link. Any other role opening the route
is sent to the dashboard. The screen only reads: it has no form and no actions.

## States

| State | What the user sees |
|---|---|
| Empty | Does not occur: the signed-in administrator is always listed |
| Loading | A skeleton of the table |
| Error | `403`: "No tienes permiso para ver esta sección." Network or `500`: "No pudimos cargar los usuarios." with *Reintentar* |
| Populated | The table above, 20 accounts per page, with the pager when there is more than one page |

## Validation rules

The screen has no input to validate.

---

**Related:** [`../design-system.md`](../design-system.md) · [`./dashboard.md`](./dashboard.md)
