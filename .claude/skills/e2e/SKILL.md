---
name: e2e
description: Run or extend the LearnHub Playwright end-to-end tests (browser tests against the real React UI, API and the learnhub_e2e MySQL database). Use after changes to backend/ or frontend/, or when asked to add an E2E test.
argument-hint: "[smoke|full|report|new <feature>]"
---

# LearnHub E2E (Playwright)

The suite lives in `e2e/`. It resets the `learnhub_e2e` database, starts the API (port 5080) and the Vite frontend (port 5173) itself, and drives Chromium. `playwright.config.ts` sets `LEARNHUB_DB`, `LEARNHUB_NOW=2026-10-01T12:00:00Z` and `DOTNET_ROLL_FORWARD=Major`; do not set them by hand.

## Safety rules
- Use only the MySQL user `test` / `test` and the database `learnhub_e2e`. Never use root.
- Never run `db/reset.sh`, `db/reset.ps1` or load `db/schema.sql` / `db/seed.sql` directly: they drop the live `learnhub` database.
- Do not edit app code (`backend/`, `frontend/`) to make a test pass. If a test exposes an app bug, stop and report it.

## 1. Check preconditions
Run: `MYSQL_PWD=test mysql --protocol=TCP -h 127.0.0.1 -P 3306 -u test -e "SELECT 1" learnhub_e2e`
If it fails, stop and tell the user an admin must run:
```sql
CREATE DATABASE IF NOT EXISTS learnhub_e2e CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER IF NOT EXISTS 'test'@'%' IDENTIFIED BY 'test';
GRANT ALL PRIVILEGES ON learnhub_e2e.* TO 'test'@'%';
FLUSH PRIVILEGES;
```
If `e2e/node_modules` is missing, run `cd e2e && npm install`. If Playwright reports a missing browser, run `cd e2e && npx playwright install chromium`.

## 2. Run (argument selects the mode, default `smoke`)
- `smoke`: `cd e2e && npm run test:smoke` (tests tagged `@smoke`: refund and checkout).
- `full`: `cd e2e && npm test`.
- `report`: `cd e2e && npm run report`.

## 3. Report results
Give pass/fail counts per spec. For each failure read the error and the trace/screenshot under `e2e/test-results`, and say whether it looks like an app bug or a test problem (locator, data, timing). Fix test code only when the user asks.

## 4. Add a test (`new <feature>`)
Use `e2e/tests/refund.spec.ts` as the template and `e2e/tests/helpers.ts` for shared helpers.
- One spec file per feature in `e2e/tests/`.
- A unique student name per test (`e2e-${Date.now()}-<random>`); the app has no auth, so the name isolates data.
- Use API calls only for setup; do the action under test and the assertions through the UI.
- Prefer `getByRole` / `getByLabel` / `getByPlaceholder`; add a `data-testid` only when a locator is ambiguous, and keep that edit minimal.
- Tag fast, critical paths `@smoke` in the test title. Never wait with fixed timeouts; use Playwright auto-waiting and `expect`.
- Run the new spec twice to confirm it is repeatable.

## Working agreement
Follow the user's standing preference: write a plan, get approval for each plan, and delegate the file edits and command runs to a Haiku subagent.
