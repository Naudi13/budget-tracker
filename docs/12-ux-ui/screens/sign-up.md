# Sign Up — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Create the business account |
| Serves | [`HU-01`](../../04-requirements/stories/HU-01-sign-in.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌──────────────────────────────────────┐
│       Create your business account   │
│  Name              [______________]  │
│  Email             [______________]  │
│  Password          [______________]  │
│  Repeat password   [______________]  │
│                [ Create account ]    │  ← primary button
│  Already have an account?  Sign in   │  ← link to sign-in.md
└──────────────────────────────────────┘
```

Flow: on `201` from `POST /users` the user is sent to [`sign-in.md`](./sign-in.md)
with the email filled in and a success banner "Account created. Sign in to start."
V1 has one account per business, with no roles.

## States

| State | What the user sees |
|---|---|
| Empty | All fields blank, the cursor in *Name* |
| Loading | *Create account* disabled and labelled "Creating…" while the request is in flight |
| Error | `409`: inline under *Email*, "An account already uses this email. Sign in instead." Network or `500`: "The account could not be created. Try again." with *Retry*; the form is not cleared |
| Populated | The filled form, ready to submit |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Name | Required, at least one character | "Enter your name or your business's name." |
| Email | Required, a valid email address | "Enter an email, for example owner@example.com." |
| Password | Required, at least 8 characters (`identity.yaml`) | "Use at least 8 characters." |
| Repeat password | Equal to *Password*; checked in the browser only | "The two passwords do not match." |

---

**Related:** [`../design-system.md`](../design-system.md) · [`./sign-in.md`](./sign-in.md)
