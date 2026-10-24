# LearnHub

LearnHub is a small e-learning platform. Students browse courses, enroll, take quizzes, track progress, earn
certificates, bookmark courses and buy paid courses from a shop (cart, bundle discount, coupons, orders, refunds).
Students can also review courses, ask questions, submit assignments and see a personal dashboard. Instructors have a
dashboard, an assignment grading page, a revenue report and a monthly payout report.

The backend is a .NET 8 Minimal API on MySQL. The frontend is React with Vite.

Exercises: see EXERCISES.md.

## What you need

- .NET 8 SDK
- Node 20 or newer
- MySQL 8 (installed on your machine or VM, or run in Docker)

## Load the database

The database is built from two files in `db/`: `schema.sql` (tables) and `seed.sql` (sample data).
The scripts drop and re-create the `learnhub` database every time you run them, then print the row count of every table.

```bash
bash db/reset.sh          # macOS, Linux, Git Bash
```

```powershell
./db/reset.ps1            # Windows PowerShell
```

Both scripts need the `mysql` command line client and read these environment variables:

| Variable | Default |
|---|---|
| `LEARNHUB_DB_HOST` | `127.0.0.1` |
| `LEARNHUB_DB_PORT` | `3306` |
| `LEARNHUB_DB_USER` | `root` |
| `LEARNHUB_DB_PASSWORD` | `Root123$` |



Expected row counts:

| Table | Rows | Table | Rows |
|---|---|---|---|
| instructors | 12 | orders | 1200 |
| students | 300 | order_lines | 1990 |
| courses | 40 | refunds | 150 |
| course_prerequisites | 13 | coupons | 25 |
| lessons | 325 | reviews | 2000 |
| quiz_questions | 650 | questions | 600 |
| enrollments | 2956 | answers | 820 |
| lesson_progress | 20242 | assignments | 120 |
| bookmarks | 700 | submissions | 900 |
| cart_items | 25 | | |

The sample data covers the twelve months before 2026-10-01.

## Run the backend

```bash
cd backend/LearnHub.Api
dotnet run --urls http://localhost:5080
```

The API listens on port 5080. The connection string is in `appsettings.json` (`ConnectionStrings:LearnHub`,
default `Server=127.0.0.1;Port=3306;User ID=root;Password=LearnHub!23;Database=learnhub`).
To use another server or port, set the `LEARNHUB_DB` environment variable to a full connection string; it wins over
the file:

```bash
LEARNHUB_DB='Server=127.0.0.1;Port=3307;User ID=root;Password=LearnHub!23;Database=learnhub' dotnet run --urls http://localhost:5080
```

Recommended: set `LEARNHUB_NOW=2026-10-01T12:00:00Z` so the app treats that moment as "now"; the sample data ends there, so without it deadlines and refund windows are judged against the real date.

## Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173. Requests to `/api` are forwarded to the backend on port 5080.
Type a student name from the data (for example the name of any row in the `students` table) to see their pages.

## Run the tests

```bash
cd backend
dotnet test
```

The tests use an in-memory SQLite database with a small fixed data set, so they need no MySQL.

## Folder map

```
db/
  schema.sql, seed.sql      tables and sample data
  reset.sh, reset.ps1       reload the database and print row counts
backend/
  LearnHub.Api/
    Program.cs              setup and routes
    appsettings.json        connection string
    Endpoints/              one file per area, maps URLs to services
    Services/               the business logic (courses, shop, reviews, Q&A, assignments, dashboards)
    Reports/                PayoutReportBuilder, the monthly payout report
    Data/LearnHubDbContext  table and column mapping
    Models/                 database entities
  LearnHub.Tests/           xUnit tests, TestData, TestEnv, CustomWebApplicationFactory
frontend/
  src/pages/                one file per page
  src/components/           NavBar, CourseCard, ReviewsSection, QaSection
  src/api.js                every call to the backend
```
