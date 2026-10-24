# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

LearnHub is a small e-learning platform: .NET 8 Minimal API (`backend/`) on MySQL 8, React + Vite frontend (`frontend/`). `EXERCISES.md` holds the Day 5 exercises (data/code audits such as refund policy and coupon abuse, each expecting a report in `reports/`, a code fix and tests). Treat "today" as 2026-10-01.

## Commands

```bash
# Database (drops and recreates `learnhub`, loads db/schema.sql + db/seed.sql, prints row counts; needs mysql client)
bash db/reset.sh            # or ./db/reset.ps1 on PowerShell
# env: LEARNHUB_DB_HOST/PORT/USER/PASSWORD (defaults 127.0.0.1, 3306, root, LearnHub!23)

# Backend (port 5080)
cd backend/LearnHub.Api && dotnet run --urls http://localhost:5080

# Tests (in-memory SQLite, no MySQL needed)
cd backend && dotnet test
cd backend && dotnet test --filter "FullyQualifiedName~ShopTests"          # one class
cd backend && dotnet test --filter "FullyQualifiedName~ShopTests.MethodName" # one test

# Frontend (http://localhost:5173, proxies /api to :5080)
cd frontend && npm install && npm run dev

# E2E (Playwright; resets learnhub_e2e only, never learnhub; starts API + frontend itself)
cd e2e && npm install && npm run test:smoke   # @smoke tests (refund, checkout)
cd e2e && npm test                            # all E2E tests
```

Environment variables for the API:
- `LEARNHUB_DB`: full connection string; overrides `ConnectionStrings:LearnHub` in `appsettings.json`.
- `LEARNHUB_NOW=2026-10-01T12:00:00Z`: pins the clock. Set it when running against seed data, otherwise deadlines and refund windows are judged against the real date.

## Architecture

- **Layering**: `Program.cs` registers services (scoped) and calls one `Map*Endpoints()` extension per area. `Endpoints/*` only translate HTTP to service calls and map error strings to status codes; business rules live in `Services/*` (Course, Progress, Enrollment, Quiz, Bookmark, Shop, Review, Qa, Assignment, Instructor, StudentDashboard). `Reports/PayoutReportBuilder` is the monthly payout report. Entities are in `Models/Entities.cs`, mapping in `Data/LearnHubDbContext.cs`, DTOs in `Services/Dtos.cs`.
- **Services return results, not exceptions**: e.g. `ShopService.AddToCart` returns `(Item, Error)`, and endpoints pick Conflict/BadRequest by matching the error text. Changing an error message can change the HTTP status.
- **No auth**: students are identified by `studentName` (query/body), resolved case-insensitively by `StudentDirectory` (`Services/Infrastructure.cs`); `GetOrCreate` creates a student on demand.
- **Time**: always use the injected `IClock` (`UtcNow`), never `DateTime.UtcNow`, so tests and `LEARNHUB_NOW` work. Money helpers are in `Infrastructure.cs` (`Money.Round` = 2 dp, away from zero).
- **Tests** (`backend/LearnHub.Tests`): `TestEnv` builds an in-memory SQLite DB, seeds the small fixed `TestData`, and constructs every service by hand with a `FakeClock`. **When adding a service or changing a constructor, update both `Program.cs` and `TestEnv.cs`.** `CustomWebApplicationFactory` is used for HTTP-level tests (`ApiTests`).
- **Two database providers**: production is MySQL (Pomelo, server version pinned to 8.4 in `Program.cs`), tests are SQLite via `EnsureCreated()` from the EF model. The real schema is `db/schema.sql`, so keep it consistent with the EF mapping, and avoid provider-specific queries.
- **MCP server** (`mcp/src/server.ts`, registered in `.mcp.json`): Node/TypeScript stdio server exposing `read_query`, `write_query` (INSERT/UPDATE only, DELETE and DDL rejected, WHERE required for UPDATE) and `list_tables` against MySQL. Build with `cd mcp && npm install && npm run build` (runs `mcp/dist/server.js`); uses the same `LEARNHUB_DB_*` variable names as `db/reset.sh`, but the server's default password (`Root123$`) differs from the script's.
- **Frontend**: one file per page in `src/pages/`, shared pieces in `src/components/`, and all backend calls go through `src/api.js`.
