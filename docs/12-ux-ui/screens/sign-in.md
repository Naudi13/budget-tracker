# Sign In — Screen

## Identification

| Field | Value |
|---|---|
| Screen | Sign in |
| Serves | [`HU-01`](../../04-requirements/stories/HU-01-sign-in.md) |
| Wireframe | Inline sketch below — no design file exists for this screen |

```
┌──────────────────────────────────┐
│          Budget Tracker          │
│  Email     [__________________]  │
│  Password  [__________________]  │
│            [     Sign in      ]  │  ← primary button
│  No account yet?  Create one     │  ← link to sign-up.md
└──────────────────────────────────┘
```

Flow: on success the bearer token from `POST /sessions` is kept in memory and in
`sessionStorage`, and the user lands on the dashboard. Every other screen
redirects here when there is no token or a request answers `401`.

## States

| State | What the user sees |
|---|---|
| Empty | Both fields blank, the cursor in *Email* |
| Loading | *Sign in* disabled and labelled "Signing in…" while the request is in flight |
| Error | `401`: banner "The email or password is not correct." — the email is kept, the password is cleared. Network or `500`: "We could not sign you in. Try again." with *Retry* |
| Populated | The filled form, ready to submit |

## Validation rules

| Field | Rule | Message shown |
|---|---|---|
| Email | Required, a valid email address | "Enter the email you signed up with, for example owner@example.com." |
| Password | Required | "Enter your password." |

The message for a wrong password never says which of the two fields is wrong,
so it does not reveal which emails have an account.

---

**Related:** [`../design-system.md`](../design-system.md) · [`./sign-up.md`](./sign-up.md)
