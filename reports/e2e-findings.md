# E2E Suite Findings

## Summary

A Playwright E2E suite was built in `e2e/`. Results: 4 of 4 smoke tests and 5 of 5 full tests pass. The browser tests found no application defect. The findings below come from the environment, test-ability and risks found while building the suite, plus the earlier refund audit (`reports/exercise-1-refund-audit.md`).

Totals: 2 critical, 5 major, 5 minor.

## Critical

1. **Live-data loss risk from reset scripts.** `db/schema.sql` starts with `DROP DATABASE IF EXISTS learnhub`, and `db/seed.sql` uses `USE learnhub`. Running `db/reset.sh` or `db/reset.ps1` for testing would wipe the live database.
   - Impact: Loss of all live data if a reset script is run against the production database.
   - Status: Mitigated for E2E. The suite uses a separate database, `learnhub_e2e`, and a MySQL user `test` with rights only on that database. A database-name guard is in `e2e/scripts/reset-e2e-db.sh`.
   - Remaining open part: `db/schema.sql` itself is still dangerous.
   - Suggested action: Add a database-name guard or separate dev/test names in `db/schema.sql`.

2. **Refunds paid against policy.** The audit found 9 of 150 orders, totalling 544.16, refunded outside the 7-day window or the progress rule:
   - 6 orders refunded after 7 days, totalling 361.76.
   - 3 orders refunded with progress of 30% or more, totalling 182.40.
   - Coupon FLASH40 orders, totalling 299.16.
   - Root cause: `RefundOrder` measured the 7-day window from the order's `UpdatedAt` instead of `CreatedAt`.
   - Impact: Refunds were paid that the policy does not allow. The money has not been recovered.
   - Status: Fixed in code at `backend/LearnHub.Api/Services/ShopService.cs` line 308, with unit tests. The affected money was not recovered and the data was not changed.

## Major

1. **Orders with `updated_at` different from `created_at`.** 94 orders with status Paid have `updated_at` different from `created_at`. In 66 of them the difference is more than 7 days.
   - Impact: The application's refund window depends on `updated_at`, so the column's values are unreliable for policy decisions. Something outside the application writes this column.
   - Status: Open. The writer is unexplained.
   - Suggested action: Identify what writes `orders.updated_at` and restrict or document it.

2. **Refunds delete progress evidence.** Refunding deletes the `lesson_progress` rows. The `refunds` row stores no order age and no progress value.
   - Impact: The record of progress at refund time is lost. The progress part of the audit is therefore a lower bound.
   - Status: Open. The user chose a code-only fix with no schema change.
   - Suggested action: Store order age and progress on each refund row in a future schema change.

3. **Over-progress refunds made outside the API.** Orders 247, 435 and 838 were refunded above the progress limit, and their progress rows still exist. The API deletes progress rows on refund, so these refunds were made by a direct database write.
   - Impact: Nothing in the database enforces the refund policy, so out-of-band writes can bypass it.
   - Status: Open.
   - Suggested action: Add a database-level or service-level control so refunds can only be made through the API, and review who has write access to the orders and refunds tables.

4. **ApiTests fail under the installed runtime.** Only the .NET 10 runtime is installed, while the API and tests target `net8.0`. Runs therefore need `DOTNET_ROLL_FORWARD=Major`. With that setting, 4 ApiTests fail because the response body is not JSON.
   - Impact: The backend test suite does not pass cleanly in this environment.
   - Status: Open. The cause was not investigated. It was not shown whether these tests also fail without the roll-forward setting.
   - Suggested action: Investigate the 4 ApiTests failures, and install the .NET 8 runtime or SDK to remove the need for roll-forward.

5. **CI workflow has never run.** `.github/workflows/ci.yml` has never run on GitHub. The following risks are unverified:
   - The mysql client 8.0 authenticating to the `mysql:8.4` service as user `test`.
   - Host `'%'` matching for the `test` user.
   - The choice of Node 24.
   - Impact: The CI pipeline may fail on first run.
   - Status: Open (unverified).
   - Suggested action: Run the CI workflow once on GitHub and fix any failures.

## Minor

1. **Ambiguous "Your name" labels.** The course detail page has three inputs labelled "Your name" (enroll, reviews and Q&A).
   - Impact: Ambiguous for assistive technology and for tests. The tests scope each input by its form heading.
   - Status: Mitigated in the tests. The labels are unchanged in the application.
   - Suggested action: Give each input a distinct label.

2. **"Total" matches "Subtotal".** In the cart summary, the text "Total" also matches "Subtotal". The frontend has no `data-testid` attributes.
   - Impact: Text-based selectors can match the wrong cell. The tests use exact cell text.
   - Status: Mitigated in the tests.
   - Suggested action: Add `data-testid` attributes to the cart summary cells.

3. **Inconsistent credentials.** `db/reset.sh` and the backend `appsettings.json` default to the root password `LearnHub!23`, which the local MySQL rejected. `CLAUDE.md` says the MCP server default is `Root123$`, which was not tried. The E2E suite uses the dedicated user `test`.
   - Impact: Setup fails with the documented defaults, and the defaults are not consistent across files.
   - Status: Mitigated for E2E by the dedicated `test` user. The defaults are unchanged.
   - Suggested action: Align the default credentials across the reset script, `appsettings.json` and `CLAUDE.md`, and remove root defaults.

4. **7-day test does not cover subtle drift.** The 7-day E2E test uses a seeded order dated 2025-10-04, about 12 months old. It would not catch a subtle `UpdatedAt` drift like the original bug. The unit test covers that case. The planned check of reverting the fix and confirming that a test fails was not done.
   - Impact: The E2E suite alone would not detect a regression of the refund-window bug.
   - Status: Mitigated by the unit test. The revert check is not done.
   - Suggested action: Run the revert check: revert the fix, confirm a test fails, then restore the fix.

5. **Test hygiene.**
   - The Windows mysql client prints CRLF line endings. The reset script strips them.
   - A full E2E run adds orders and refunds to `learnhub_e2e`. The database is reset at the start of every run.
   - The Playwright browser build was missing and needed `npx playwright install chromium`.
   - Impact: Minor setup friction and test data accumulation within the E2E database only.
   - Status: Mitigated. The reset script handles CRLF and the database reset, and the browser install is a documented setup step.

## Recommendations

- Add a database-name guard, or separate dev and test database names, in `db/schema.sql`.
- Find what writes `orders.updated_at`.
- Store order age and progress on each refund row.
- Run the CI workflow once on GitHub.
- Investigate the 4 ApiTests failures.
- Decide on the post-edit smoke-test hook and the revert check.
