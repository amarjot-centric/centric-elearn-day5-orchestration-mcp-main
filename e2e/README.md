# LearnHub E2E tests

Playwright tests that drive the React UI against the real API and MySQL.

## Setup (once)

```bash
cd e2e && npm install
npx playwright install chromium   # only if the browser is missing
```

## Run

```bash
cd e2e
npm run test:smoke   # @smoke tests only (refunds, checkout)
npm test             # all tests
npm run report       # open the last HTML report
```

Each run resets the database `learnhub_e2e` (never `learnhub`) from `db/schema.sql` and `db/seed.sql`, starts the API on port 5080 and the frontend on port 5173, then runs the tests with one worker.

## Database

- Connection: `LEARNHUB_DB_HOST` / `LEARNHUB_DB_PORT` / `LEARNHUB_DB_USER` / `LEARNHUB_DB_PASSWORD` (defaults 127.0.0.1, 3306, `test`, `test`).
- The `test` user needs ALL privileges on `learnhub_e2e` only.
- Existing servers are never reused, so a server on port 5080 or 5173 must be stopped first.
