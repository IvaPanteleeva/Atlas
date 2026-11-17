# Atlas ERP

A small ERP for Atlas, a renovation subcontractor. It replaces a handwritten note per job site with a mobile-first app: sites, work days with priced items, client payments, the crew and what they earn, tasks, services and site prices, and a read-only portal for the client company.

Built as the exam project for the course "AI-Assisted Development". All code was written by Claude Code; I made the decisions, reviewed and tested.

## Features
- **Sites** with status (lead, active, done), company or end customer, address, map link and contacts.
- **Services and site prices**: a work item takes the site price, then the default price, otherwise it is marked "price?".
- **Work days** with items (quantity × rate) and the people who worked, with pay per person.
- **Payments and sums** computed on the server: earned, paid, remaining, items to review, worker cost and what is left for us.
- **People**: crew and contacts, days worked and earnings.
- **Tasks** per site or for the company, with an optional assignee.
- **Roles**: Owner, Manager and ClientViewer (ASP.NET Core Identity, cookie auth).
- **Client portal** (`/portal`): a client user sees only the sites of their own company, filtered on the server, without internal notes, people or costs.

## Stack
- **Backend:** .NET 10 Minimal APIs, EF Core with PostgreSQL, FluentValidation, xUnit with Testcontainers.
- **Frontend:** Angular 22 (zoneless, signals), NgRx Signal Store, ng-zorro-antd, Vitest. UI in Bulgarian, currency EUR.

```
src/Atlas.Api             Minimal API endpoints, auth, static files (Angular build goes to wwwroot)
src/Atlas.Application     DTOs, Result, validators
src/Atlas.Infrastructure  entities, DbContext, migrations, repositories
tests/Atlas.UnitTests     unit tests with hand-written fakes
tests/Atlas.IntegrationTests  API tests against a real Postgres (Testcontainers)
web/                      Angular app
```

## Run locally
Requirements: .NET 10 SDK, Node 22, Docker.

```bash
docker compose up -d
```

```bash
dotnet run --project src/Atlas.Api
```

```bash
cd web && npm ci && npm start
```

Open http://localhost:4200 and sign in with the local owner from `src/Atlas.Api/appsettings.Development.json`. The API runs on http://localhost:5002 and applies migrations on startup in Development; the API reference (Scalar) is at http://localhost:5002/scalar.

## Tests
```bash
dotnet test Atlas.slnx
```

```bash
cd web && npx ng test --watch=false
```

Integration tests need Docker. GitHub Actions runs the build and all tests on every push and pull request to `main`.
