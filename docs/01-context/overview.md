# 01 — Overview

## What it does

A single deployable web application that lets a small business or an
entrepreneur record income and expenses and read the state of their finances
from one dashboard, from one codebase, one database, and one release pipeline.

| Capability | Description |
|---|---|
| Income tracking | Register a money-in transaction with amount, date, category, and description |
| Expense tracking | Register a money-out transaction with amount, date, category, and description |
| Balance | Compute the net balance as total income minus total expenses |
| Dashboard | A single view with KPIs (total income, total expenses, net balance) for a fast read of financial health |

## Who it is for

| Audience | Uses it to |
|---|---|
| Business owner / entrepreneur | Record daily income and expenses and know the current balance without a spreadsheet |
| Small business staff | Keep the transaction log accurate as money moves in and out |

## Problem it solves

Before this system, entrepreneurs and small businesses track income and
expenses in scattered spreadsheets or notebooks, updated inconsistently.
Cash-flow problems are noticed only after the fact, because no single view
answers "how much did we make, how much did we spend, and what is left"
without manually adding up several sheets.

## Constraints

| Constraint | Why |
|---|---|
| One deployable application | A single small team or solo founder, no operating budget for running several deployments |
| One relational database | Income, expenses, and balance must stay consistent within one transaction |
| V1 scoped to recording and reading, not planning | The first release proves the core loop (register, then read); budgeting and forecasting come later |

---

**Related:** [`./scope.md`](./scope.md) · [`./glossary.md`](./glossary.md)
