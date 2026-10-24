#!/usr/bin/env bash
# Loads db/schema.sql and db/seed.sql into MySQL and prints the row count of every table.
# Settings (environment variables): LEARNHUB_DB_HOST, LEARNHUB_DB_PORT, LEARNHUB_DB_USER, LEARNHUB_DB_PASSWORD
set -euo pipefail

HOST="${LEARNHUB_DB_HOST:-127.0.0.1}"
PORT="${LEARNHUB_DB_PORT:-3306}"
USER_NAME="${LEARNHUB_DB_USER:-root}"
PASSWORD="${LEARNHUB_DB_PASSWORD:-LearnHub!23}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export MYSQL_PWD="$PASSWORD"
MYSQL=(mysql --protocol=TCP -h "$HOST" -P "$PORT" -u "$USER_NAME" --default-character-set=utf8mb4)

echo "Loading schema into $HOST:$PORT ..."
"${MYSQL[@]}" < "$HERE/schema.sql"
echo "Loading seed data ..."
"${MYSQL[@]}" learnhub < "$HERE/seed.sql"

echo "Row counts:"
TABLES=(instructors students courses course_prerequisites lessons quiz_questions enrollments lesson_progress bookmarks cart_items coupons orders order_lines refunds reviews questions answers assignments submissions)
for t in "${TABLES[@]}"; do
  n=$("${MYSQL[@]}" -N -B learnhub -e "SELECT COUNT(*) FROM $t")
  printf "  %-22s %s\n" "$t" "$n"
done
