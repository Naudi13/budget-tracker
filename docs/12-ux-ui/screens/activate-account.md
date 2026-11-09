# Activate Account and Reset Password — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Choose a password, at `/activate#token=…` and `/reset-password#token=…` |
| Serves | [`HU-10`](../../04-requirements/stories/HU-10-activate-account-or-reset-password.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌──────────────────────────────────────────┐
│           Activa tu cuenta               │  ← "Restablece tu contraseña" on reset
│  Contraseña           [______________]   │
│  Repite la contraseña [______________]   │
│                       [   Guardar    ]   │  ← primary button
└──────────────────────────────────────────┘
```

Flow: on `204` the user goes to [`sign-in.md`](./sign-in.md) with the banner
"Contraseña guardada. Inicia sesión."

## States

| State | What the user sees |
|---|---|
| Empty | Both fields blank, the cursor in *Contraseña* |
| Loading | *Guardar* disabled and labelled "Guardando…" |
| Error | `400` on `token`, or a link with no token: "Este enlace no es válido o ya venció. Pide uno nuevo a tu administrador." Network or `500`: "No pudimos guardar tu contraseña. Inténtalo de nuevo." |
| Populated | The filled form, ready to submit |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Password (*Contraseña*) | Required, 8 to 128 characters | "Usa al menos 8 caracteres." / over 128: "Usa máximo 128 caracteres." |
| Repeat password | Equal to *Contraseña*; checked in the browser only | "Las contraseñas no coinciden." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`./sign-in.md`](./sign-in.md)
