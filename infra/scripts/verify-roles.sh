#!/bin/sh
# Proves the least-privilege rule of 00-governance/security-policy.md against
# the running `postgres` service. Used by CI and by a developer after
# `docker compose up -d --wait`. Needs no psql on the host: every query runs
# inside the container, over TCP with a password, as the API and the
# migrations would connect.
#
# Usage: sh scripts/verify-roles.sh      (from the repository root, .env present)
set -eu

[ -f .env ] || { echo "missing .env — copy .env.example to .env" >&2; exit 1; }
set -a; . ./.env; set +a

DB="${POSTGRES_DB:-app_dev}"
failures=0

# run ROLE PASSWORD SQL — prints psql's output, returns psql's exit code
run() {
  docker compose exec -T -e PGPASSWORD="$2" postgres \
    psql -v ON_ERROR_STOP=1 -X -q -t -A -h postgres -U "$1" -d "$DB" -c "$3" 2>&1
}

expect_ok() {
  if out=$(run "$2" "$3" "$4"); then echo "PASS  $1"
  else echo "FAIL  $1: $out"; failures=$((failures + 1)); fi
}

expect_refused() {
  if out=$(run "$2" "$3" "$4"); then echo "FAIL  $1: it was allowed"; failures=$((failures + 1))
  else echo "PASS  $1"; fi
}

M=bud_tack_migrator; MP="$BUD_TACK_MIGRATOR_PASSWORD"
A=bud_tack_app;      AP="$BUD_TACK_APP_PASSWORD"

expect_ok      "migrator signs in with its password"   "$M" "$MP" "SELECT current_user"
expect_ok      "app signs in with its password"        "$A" "$AP" "SELECT current_user"
expect_refused "app is refused with a wrong password"  "$A" "wrong-password" "SELECT 1"
expect_ok      "migrator owns the database"            "$M" "$MP" \
  "DO \$\$ BEGIN IF (SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname = current_database()) <> 'bud_tack_migrator' THEN RAISE EXCEPTION 'owner is not bud_tack_migrator'; END IF; END \$\$"
expect_ok      "migrator can create a schema"          "$M" "$MP" \
  "BEGIN; CREATE SCHEMA verify_probe; ROLLBACK;"
expect_refused "app cannot create a schema"            "$A" "$AP" "CREATE SCHEMA verify_probe"
expect_refused "app cannot create a table in public"   "$A" "$AP" "CREATE TABLE public.verify_probe (id int)"

if [ "$failures" -gt 0 ]; then
  echo "$failures check(s) failed" >&2
  exit 1
fi
echo "All role checks passed."
