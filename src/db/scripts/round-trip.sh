#!/bin/sh
# The rollback proof of 06-data/migrations.md: migrate up, down to version 0,
# and up again against the same database; the second `up` must succeed and
# leave exactly the schema the first one did. Also checks the grants the
# first migration gives the API's role.
#
# Needs: a built BudTack.Db.Migrations (dotnet build -c Release), a reachable
# database in MIGRATOR_DATABASE_URL (or .env), and the PostgreSQL container
# named in PG_CONTAINER, used to run psql and pg_dump without installing them.
#
# Usage: PG_CONTAINER=bud-tack-postgres-1 sh scripts/round-trip.sh
set -eu

PG_CONTAINER="${PG_CONTAINER:-bud-tack-postgres-1}"
PG_SUPERUSER="${PG_SUPERUSER:-postgres}"
PG_DB="${PG_DB:-app_dev}"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

migrate() {
  echo "── migrate $1"
  dotnet run -c Release --no-build --project src/BudTack.Db.Migrations -- "$1" > "$WORK/$1.log" 2>&1 \
    || { cat "$WORK/$1.log"; echo "FAIL  migrate $1" >&2; exit 1; }
}

sql() {
  docker exec -i "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -X -q -t -A -U "$PG_SUPERUSER" -d "$PG_DB" -c "$1"
}

# pg_dump 17.6+ prints a random \restrict key per run; drop it before comparing.
dump() {
  docker exec "$PG_CONTAINER" pg_dump --schema-only --no-owner -U "$PG_SUPERUSER" "$PG_DB" \
    | grep -v -E '^\\(un)?restrict ' > "$1"
}

check() {
  if [ "$(sql "$2")" = "t" ]; then echo "PASS  $1"; else echo "FAIL  $1" >&2; exit 1; fi
}

migrate up
dump "$WORK/first-up.sql"
check "schemas identity and transactions exist" \
  "SELECT count(*) = 2 FROM pg_namespace WHERE nspname IN ('identity', 'transactions')"
check "app role may use both schemas" \
  "SELECT has_schema_privilege('bud_tack_app', 'identity', 'USAGE') AND has_schema_privilege('bud_tack_app', 'transactions', 'USAGE')"
check "app role may not create in either schema" \
  "SELECT NOT has_schema_privilege('bud_tack_app', 'identity', 'CREATE') AND NOT has_schema_privilege('bud_tack_app', 'transactions', 'CREATE')"
# Default privileges only show on a real table created by the migrator, so
# create one inside a transaction that is rolled back.
docker exec -i "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -X -q -t -A -U "$PG_SUPERUSER" -d "$PG_DB" > "$WORK/probe.out" <<'EOSQL'
BEGIN;
SET LOCAL ROLE bud_tack_migrator;
CREATE TABLE transactions.round_trip_probe (id int);
SELECT has_table_privilege('bud_tack_app', 'transactions.round_trip_probe', 'SELECT, INSERT, UPDATE, DELETE');
ROLLBACK;
EOSQL
if grep -qx t "$WORK/probe.out"; then echo "PASS  tables created by the migrator are writable by the app role"
else echo "FAIL  tables created by the migrator are writable by the app role" >&2; exit 1; fi

migrate down
check "down removes both schemas" \
  "SELECT count(*) = 0 FROM pg_namespace WHERE nspname IN ('identity', 'transactions')"

migrate up
dump "$WORK/second-up.sql"
if diff -u "$WORK/first-up.sql" "$WORK/second-up.sql"; then
  echo "PASS  second up leaves the same schema as the first"
else
  echo "FAIL  second up left a different schema" >&2; exit 1
fi

echo "Round trip passed."
