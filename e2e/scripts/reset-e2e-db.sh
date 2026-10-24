#!/usr/bin/env bash
# Resets the E2E database `learnhub_e2e` from db/schema.sql and db/seed.sql.
# Never touches the `learnhub` database: every `learnhub` reference is rewritten to `learnhub_e2e`,
# and the DROP/CREATE DATABASE lines are removed (the restricted test user cannot create databases).
# Settings (environment variables): LEARNHUB_DB_HOST, LEARNHUB_DB_PORT, LEARNHUB_DB_USER, LEARNHUB_DB_PASSWORD
set -euo pipefail

DB_NAME="learnhub_e2e"
if [[ "$DB_NAME" != "learnhub_e2e" ]]; then
  echo "Refusing to run: target database must be exactly learnhub_e2e (got '$DB_NAME')." >&2
  exit 1
fi

HOST="${LEARNHUB_DB_HOST:-127.0.0.1}"
PORT="${LEARNHUB_DB_PORT:-3306}"
USER_NAME="${LEARNHUB_DB_USER:-test}"
PASSWORD="${LEARNHUB_DB_PASSWORD:-test}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DB_DIR="$HERE/../../db"

export MYSQL_PWD="$PASSWORD"
MYSQL=(mysql --protocol=TCP -h "$HOST" -P "$PORT" -u "$USER_NAME" --default-character-set=utf8mb4)

# Rewrite the SQL: learnhub -> learnhub_e2e, and drop DATABASE create/drop statements.
rewrite() {
  sed -e 's/learnhub/learnhub_e2e/g' \
      -e '/^DROP DATABASE/d' \
      -e '/^CREATE DATABASE/d' "$1"
}

# Safety net: after rewriting, no bare `learnhub` database reference may remain.
if { rewrite "$DB_DIR/schema.sql"; rewrite "$DB_DIR/seed.sql"; } | grep -E 'learnhub([^_]|$)' >/dev/null; then
  echo "Refusing to run: rewritten SQL still references a non-e2e 'learnhub' database." >&2
  exit 1
fi

echo "Dropping all tables in $DB_NAME on $HOST:$PORT (user $USER_NAME) ..."
# tr strips the CRLF line endings the Windows mysql client prints.
TABLE_LIST="$("${MYSQL[@]}" -N -B -e "SELECT table_name FROM information_schema.tables WHERE table_schema = '$DB_NAME' AND table_type = 'BASE TABLE'" | tr -d '\r')"
if [[ -n "$TABLE_LIST" ]]; then
  {
    echo "SET FOREIGN_KEY_CHECKS = 0;"
    while IFS= read -r t; do
      [[ -n "$t" ]] && echo "DROP TABLE IF EXISTS \`$DB_NAME\`.\`$t\`;"
    done <<< "$TABLE_LIST"
    echo "SET FOREIGN_KEY_CHECKS = 1;"
  } | "${MYSQL[@]}" "$DB_NAME"
fi

echo "Loading schema into $DB_NAME ..."
rewrite "$DB_DIR/schema.sql" | "${MYSQL[@]}" "$DB_NAME"
echo "Loading seed data into $DB_NAME ..."
rewrite "$DB_DIR/seed.sql" | "${MYSQL[@]}" "$DB_NAME"

echo "Row counts ($DB_NAME):"
TABLES=(instructors students courses course_prerequisites lessons quiz_questions enrollments lesson_progress bookmarks cart_items coupons orders order_lines refunds reviews questions answers assignments submissions)
for t in "${TABLES[@]}"; do
  n=$("${MYSQL[@]}" -N -B "$DB_NAME" -e "SELECT COUNT(*) FROM $t")
  printf "  %-22s %s\n" "$t" "$n"
done
