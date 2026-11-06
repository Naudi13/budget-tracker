# Sign Up — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Create the business account |
| Serves | [`HU-01`](../../04-requirements/stories/HU-01-sign-in.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌──────────────────────────────────────────┐
│       Crea la cuenta de tu negocio       │
│  Nombre               [______________]   │
│  Correo electrónico   [______________]   │
│  Contraseña           [______________]   │
│  Repite la contraseña [______________]   │
│                       [ Crear cuenta ]   │  ← primary button
│  ¿Ya tienes cuenta?  Inicia sesión       │  ← link to sign-in.md
└──────────────────────────────────────────┘
```

Flow: on `201` from `POST /users` the user is sent to [`sign-in.md`](./sign-in.md)
with the email filled in and a success banner "Cuenta creada. Inicia sesión para empezar."
V1 has one account per business, with no roles.

## States

| State | What the user sees |
|---|---|
| Empty | All fields blank, the cursor in *Nombre* |
| Loading | *Crear cuenta* disabled and labelled "Creando…" while the request is in flight |
| Error | `409`: inline under *Correo electrónico*, "Ya existe una cuenta con este correo. Inicia sesión." Network or `500`: "No pudimos crear la cuenta. Inténtalo de nuevo." with *Reintentar*; the form is not cleared |
| Populated | The filled form, ready to submit |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Name (*Nombre*) | Required, 1 to 100 characters | "Escribe tu nombre o el de tu negocio." / over 100: "Usa máximo 100 caracteres." |
| Email (*Correo electrónico*) | Required, a valid email address | "Escribe un correo válido, por ejemplo ana@example.com." |
| Password (*Contraseña*) | Required, 8 to 128 characters (`identity.yaml`) | "Usa al menos 8 caracteres." / over 128: "Usa máximo 128 caracteres." |
| Repeat password (*Repite la contraseña*) | Equal to *Contraseña*; checked in the browser only | "Las contraseñas no coinciden." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`./sign-in.md`](./sign-in.md)
