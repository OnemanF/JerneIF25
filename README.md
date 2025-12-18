# Lottery Application for Jerne IF Football Club

This project is a small lottery system built for **Jerne IF Football Club**. It is a distributed application with a **.NET Web API backend** and a **React + TypeScript frontend**.

The system supports two user roles — **Admin** and **Player** — and the main goal was to deliver a complete end-to-end solution with authentication, authorization, EF Core data access, and CI with automated tests.

---

## Overview

**Admin** users manage players, transactions, and the weekly lottery lifecycle (publishing winners and undoing publishes when allowed).

**Players** buy boards for the weekly lottery game and participate in draws.

The system enforces exactly **one active lottery game per week** and ensures all critical actions are role-protected.

---

## Key Features

### User Roles

#### Admin

* Create, update, soft-delete, and reactivate players
* Review deposit transactions (approve / reject)
* Publish weekly winning numbers
* Undo the last publish when business rules allow
* View weekly board purchases, winners, and summary statistics

#### Player

* Register and log in
* Buy one or more boards per active week
* View purchase status and results once winners are published

---

## Technology Stack

**Backend**

* .NET 9 Web API
* Entity Framework Core (PostgreSQL)

**Frontend**

* React
* TypeScript
* React Router
* Vite

**Authentication & Authorization**

* JWT authentication
* Role-based authorization (`admin`, `player`)

**Documentation**

* Swagger / OpenAPI
* NSwag + Scalar

**CI/CD**

* GitHub Actions (build + test)

---

## Functional Highlights

### Lottery Gameplay

* Exactly **one active game per week** (ISO week start)
* Admin publishes **3 unique winning numbers** (range 1–16)
* Publishing:

    * Closes the current week
    * Creates and activates the next week automatically

#### Undo Publish Logic

* Reopens the last closed week
* Inactivates the next week
* Undo is blocked if the next week already has board purchases

---

### Boards & Transactions

* Players can buy boards for the active week
* Deposit transaction flow:

    * Player creates transaction
    * Admin reviews and decides
    * Balance is credited **only after approval**

---

### Admin Views

* List games (active / closed / inactive)
* View boards per game
* See summary data (total boards, winners)
* Publish winners / undo last publish

---

## 🌐 Live Demo

**Application**

```
https://jerneifspil.fly.dev/login
```

> Note: If the app is sleeping, the first request may take a few seconds to spin up.

### Test Logins

**Admin**

* Email: `admin@admin.com`
* Password: `fivefive`

**Player**

Optional:
* Email: `f@gmail.com`
* Password: `ffffff`

* Email: `max@gmail.com`
* Password: `maxmax`

---

## Security & Authorization

* **Authentication**: JWT tokens
* **Authorization**: Role-based

    * `admin`: full admin endpoints (games, players, transactions)
    * `player`: own player actions only
* **Passwords**: Stored as salted hashes using **BCrypt**
* **Secrets**:

    * Database connection string and JWT secret are **not committed to Git**
    * Provided via environment variables / GitHub Secrets

---

## ⚙️ Environment & Configuration

### Backend Environment Variables

| Variable                | Description                                          |
| ----------------------- | ---------------------------------------------------- |
| `AppOptions__Db`        | PostgreSQL connection string (Fly.io / Neon / local) |
| `AppOptions__JwtSecret` | JWT signing secret (≥ 32 characters)                 |

---

## Run Locally

### Backend

```bash
# from server/api
dotnet run
```

The API reads configuration from environment variables or `appsettings`. Swagger UI is available when running locally.

### Frontend

```bash
# from client
npm install
npm run dev
```

Runs the Vite dev server with React Router and TypeScript.

---

## Testing & CI

* Test suite built with **xUnit v3**
* **XUnit.DependencyInjection** for test bootstrapping
* **Testcontainers (PostgreSQL)** for isolated, repeatable integration tests

Tests cover:

* Games service and controller
* Happy and unhappy paths
* Publish, draft, undo, and list scenarios

### Run Tests Locally

```bash
# from server/tests
dotnet test
```

The test project spins up a PostgreSQL Testcontainer, applies schema, runs tests, and tears down automatically. No data is written to your local database.

### CI

A GitHub Actions workflow runs the following on every push or pull request to `master`:

* `dotnet restore`
* `dotnet build`
* `dotnet test`

---

## ✅ What Works Today

* Admin and Player flows with authentication
* Weekly game lifecycle:

    * Active → Publish (closed + spawn next) → Undo (when allowed)
* Buying boards and computing winners
* Admin dashboards (game list, boards per game, summaries)
* Swagger / OpenAPI documentation
* Automated CI builds and test execution

---

## 🚧 Known Gaps / Next Steps

* **Payments**
  No real MobilePay / Stripe integration yet. Transactions are stored and approved by admins, but no external payment provider is connected.

