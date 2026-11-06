# Sign In — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Sign in |
| Serves | [`HU-01`](../../04-requirements/stories/HU-01-sign-in.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌──────────────────────────────────────────┐
│              Budget Tracker              │
│  Correo electrónico  [________________]  │
│  Contraseña          [________________]  │
│                      [ Iniciar sesión ]  │  ← primary button
│  ¿Aún no tienes cuenta?  Crea una        │  ← link to sign-up.md
└──────────────────────────────────────────┘
```

Flow: on success the bearer token from `POST /sessions` is kept in memory and in
`sessionStorage`, and the user lands on the dashboard. Every other screen
redirects here when there is no token or a request answers `401`.

## States

| State | What the user sees |
|---|---|
| Empty | Both fields blank, the cursor in *Correo electrónico* |
| Loading | *Iniciar sesión* disabled and labelled "Iniciando sesión…" while the request is in flight |
| Error | `401`: banner "El correo o la contraseña no son correctos." — the email is kept, the password is cleared. Network or `500`: "No pudimos iniciar tu sesión. Inténtalo de nuevo." with *Reintentar* |
| Populated | The filled form, ready to submit |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Email (*Correo electrónico*) | Required, a valid email address | "Escribe el correo con el que te registraste, por ejemplo ana@example.com." |
| Password (*Contraseña*) | Required | "Escribe tu contraseña." |

The message for a wrong password never says which of the two fields is wrong,
so it does not reveal which emails have an account.

---

**Related:** [`../design-system.md`](../design-system.md) · [`./sign-up.md`](./sign-up.md)
