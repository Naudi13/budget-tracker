# Users — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Create and list the accounts (route `/settings/users`) |
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
is sent to the dashboard.

Each row also shows the state (*Pendiente*, *Activa* or *Inactiva*) and its actions:

| State | Actions |
|---|---|
| *Pendiente* | *Generar enlace*, *Inactivar*, *Eliminar* |
| *Activa* | *Restablecer contraseña*, *Inactivar* (not on the administrator's own row) |
| *Inactiva* | *Activar*, and *Eliminar* if it was never activated |

*Inactivar* and *Eliminar* ask first: "¿Inactivar a *nombre*? No podrá entrar hasta que
la actives de nuevo." and "¿Eliminar a *nombre*? No se puede deshacer."

*Nuevo usuario* opens a form with name, email and role. On saving, and for either
action, a dialog shows the link once, with *Copiar enlace* and the warning "Copia
este enlace ahora: no se volverá a mostrar."

## States

| State | What the user sees |
|---|---|
| Empty | Does not occur: the signed-in administrator is always listed |
| Loading | A skeleton of the table |
| Error | `403`: "No tienes permiso para ver esta sección." Network or `500`: "No pudimos cargar los usuarios." with *Reintentar* |
| Populated | The table above, 20 accounts per page, with the pager when there is more than one page |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Name (*Nombre*) | Required, 1 to 100 characters | "Escribe el nombre." / over 100: "Usa máximo 100 caracteres." |
| Email (*Correo electrónico*) | Required, a valid email address; unique | "Escribe un correo válido, por ejemplo ana@example.com." / `409`: "Ya existe una cuenta con este correo." |
| Role (*Rol*) | Required: Usuario or Administrador | "Elige un rol." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`./dashboard.md`](./dashboard.md)
