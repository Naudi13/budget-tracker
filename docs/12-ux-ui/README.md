# 12 — UX/UI

> [!NOTE] INSTRUCTIONS
> Read this before filling in any document in this section.
> Delete this block once every document in the section is complete, or once
> this section has been explicitly marked not applicable, below.

## Purpose

This section is optional: skip it entirely when the system has no interface
of its own — a pure API, a batch job, a CLI. When the application does serve
screens, this section defines what keeps them consistent with each other —
shared tokens, a component inventory, and one named accessibility target —
not general design advice.

**Applies to this system:** yes — the Angular frontend in `bud-tack-portal` serves
the screens, and each one calls the single backend API directly.

## Documents

| Document | Answers | Priority |
|---|---|---|
| [design-system.md](./design-system.md) | What visual and interaction rules do screens share? | ⭐ |
| [screens/sign-in.md](./screens/sign-in.md) | How does the owner sign in, and what does a failure say? | ⭐ |
| [screens/sign-up.md](./screens/sign-up.md) | How is the business account created? | ⭐ |
| [screens/dashboard.md](./screens/dashboard.md) | What does the dashboard show, and in which states? | ⭐ |
| [screens/new-record.md](./screens/new-record.md) | How is a transaction recorded, and what does validation say? | ⭐ |
| [screens/transaction-history.md](./screens/transaction-history.md) | How are transactions listed and deleted? | ⭐ |
| [screens/categories-configuration.md](./screens/categories-configuration.md) | How are categories and subcategories created, deactivated and deleted? | ⭐ |
| [screens/budgets.md](./screens/budgets.md) | How is a budget set and listed? | — |
| [screens/users.md](./screens/users.md) | How does an administrator list the accounts? | ⭐ |
| [_template-screen.md](./_template-screen.md) | Template — copy it once per screen | — |

## Out of scope

A micro-frontend split with an independent deploy per screen or module, a
backend-for-frontend service, and an API gateway aggregating more than one
backend for a single screen. This application has exactly one backend and
one deployable; a screen calls it directly, and there is nothing to
aggregate.

Distributed-system concerns belong to the [microservices governance framework](https://github.com/jesusarielgb-works/microservices-governance-framework).

## Ready when

- [ ] A reviewer has recorded the decision on the **Applies to this system** line above — this section applies, or is explicitly skipped and why
- [ ] Every document in this section has its INSTRUCTIONS block removed
- [ ] Every screen copied from `_template-screen.md` cites a real `HU-NN` from [`../04-requirements/user-stories.md`](../04-requirements/user-stories.md), not a placeholder

---

**Related:** [`../04-requirements/README.md`](../04-requirements/README.md) · [`../09-modules/README.md`](../09-modules/README.md)
