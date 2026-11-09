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

# Identity001_CreateUsers (HU-01).
check "identity.users exists" \
  "SELECT to_regclass('identity.users') IS NOT NULL"
check "app role may read and write identity.users, and nothing more" \
  "SELECT has_table_privilege('bud_tack_app', 'identity.users', 'SELECT, INSERT, UPDATE, DELETE')
      AND NOT has_table_privilege('bud_tack_app', 'identity.users', 'TRUNCATE')"
# Each rule is tried as the app role inside a transaction that is rolled back:
# the second statement of each block must be refused.
refused() {
  if docker exec -i "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -X -q -U "$PG_SUPERUSER" -d "$PG_DB" > "$WORK/refused.out" 2>&1 <<EOSQL
BEGIN;
SET LOCAL ROLE bud_tack_app;
$2
ROLLBACK;
EOSQL
  then echo "FAIL  $1: it was allowed" >&2; exit 1
  else echo "PASS  $1"; fi
}
ok_user="INSERT INTO identity.users (id, name, email, password_hash) VALUES (gen_random_uuid(), 'Ana Owner', 'owner@example.com', 'hash');"
if docker exec -i "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -X -q -t -A -U "$PG_SUPERUSER" -d "$PG_DB" > "$WORK/allowed.out" 2>&1 <<EOSQL
BEGIN;
SET LOCAL ROLE bud_tack_app;
$ok_user
SELECT created_at IS NOT NULL AND updated_at IS NOT NULL FROM identity.users WHERE email = 'owner@example.com';
ROLLBACK;
EOSQL
then
  if grep -qx t "$WORK/allowed.out"; then echo "PASS  the app role signs up a valid user, with created_at and updated_at filled in"
  else echo "FAIL  the timestamps were not filled in" >&2; cat "$WORK/allowed.out"; exit 1; fi
else echo "FAIL  the app role could not sign up a valid user" >&2; cat "$WORK/allowed.out"; exit 1; fi
refused "a second account with the same email is refused" \
  "$ok_user INSERT INTO identity.users (id, name, email, password_hash) VALUES (gen_random_uuid(), 'Other', 'owner@example.com', 'hash');"
refused "an email that is not lowercase is refused" \
  "INSERT INTO identity.users (id, name, email, password_hash) VALUES (gen_random_uuid(), 'Ana', 'Owner@Example.com', 'hash');"
refused "a blank name is refused" \
  "INSERT INTO identity.users (id, name, email, password_hash) VALUES (gen_random_uuid(), '   ', 'blank@example.com', 'hash');"
refused "a user without a password hash is refused" \
  "INSERT INTO identity.users (id, name, email) VALUES (gen_random_uuid(), 'Ana', 'nohash@example.com');"
refused "the app role cannot drop identity.users" \
  "DROP TABLE identity.users;"

# Identity002_AddUserRole and Identity003_SeedUsers (HU-09).
check "a new account is a USER unless it says otherwise" \
  "SELECT column_default LIKE '%USER%' FROM information_schema.columns WHERE table_schema = 'identity' AND table_name = 'users' AND column_name = 'role'"
check "one seed user per role, stored with a BCrypt hash" \
  "SELECT count(*) = 2 AND count(DISTINCT role) = 2 AND bool_and(password_hash LIKE '\$2a\$12\$%') FROM identity.users WHERE id::text LIKE '00000000-0000-4000-8000-%'"
refused "a role that does not exist is refused" \
  "INSERT INTO identity.users (id, name, email, password_hash, role) VALUES (gen_random_uuid(), 'Ana', 'badrole@example.com', 'hash', 'OWNER');"

# Transactions001_CreateCategories and Transactions002_CreateSubcategories (HU-02).
allowed() {
  if docker exec -i "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -X -q -U "$PG_SUPERUSER" -d "$PG_DB" > "$WORK/allowed.out" 2>&1 <<EOSQL
BEGIN;
SET LOCAL ROLE bud_tack_app;
$2
ROLLBACK;
EOSQL
  then echo "PASS  $1"
  else echo "FAIL  $1" >&2; cat "$WORK/allowed.out" >&2; exit 1; fi
}
ana=00000000-0000-7000-8000-00000000000a
leo=00000000-0000-7000-8000-00000000000b
office=00000000-0000-7000-8000-0000000000c1
category() { # id owner name type
  echo "INSERT INTO transactions.categories (id, owned_by, name, type, created_by, updated_by) VALUES ('$1', '$2', '$3', '$4', '$2', '$2');"
}
subcategory() { # category name
  echo "INSERT INTO transactions.subcategories (id, category_id, name, created_by, updated_by) VALUES (gen_random_uuid(), '$1', '$2', '$ana', '$ana');"
}
check "transactions.categories and .subcategories exist" \
  "SELECT to_regclass('transactions.categories') IS NOT NULL AND to_regclass('transactions.subcategories') IS NOT NULL"
check "app role may read and write both, and nothing more" \
  "SELECT has_table_privilege('bud_tack_app', 'transactions.categories', 'SELECT, INSERT, UPDATE, DELETE')
      AND has_table_privilege('bud_tack_app', 'transactions.subcategories', 'SELECT, INSERT, UPDATE, DELETE')
      AND NOT has_table_privilege('bud_tack_app', 'transactions.categories', 'TRUNCATE')"
check "a subcategory's key to its category is ON DELETE RESTRICT (BR-05)" \
  "SELECT confdeltype = 'r' FROM pg_constraint WHERE conname = 'fk_subcategories_category_id'"
allowed "a category and a subcategory are created active" \
  "$(category $office $ana Oficina expense) $(subcategory $office Papelería)
   DO \$\$ BEGIN
     IF NOT (SELECT bool_and(is_active) FROM (SELECT is_active FROM transactions.categories UNION ALL SELECT is_active FROM transactions.subcategories) t)
     THEN RAISE EXCEPTION 'not active'; END IF;
   END \$\$;"
refused "a second expense category of the same owner with the same name, in any case, is refused" \
  "$(category $office $ana Oficina expense) $(category 00000000-0000-7000-8000-0000000000c2 $ana OFICINA expense)"
allowed "the same name is allowed in the other type" \
  "$(category $office $ana Oficina expense) $(category 00000000-0000-7000-8000-0000000000c2 $ana Oficina income)"
allowed "the same name is allowed for another owner" \
  "$(category $office $ana Oficina expense) $(category 00000000-0000-7000-8000-0000000000c2 $leo Oficina expense)"
refused "a type other than income or expense is refused" \
  "$(category $office $ana Oficina savings)"
refused "a blank category name is refused" \
  "$(category $office $ana '   ' expense)"
refused "a category without an owner is refused" \
  "INSERT INTO transactions.categories (id, name, type, created_by, updated_by) VALUES ('$office', 'Oficina', 'expense', '$ana', '$ana');"
refused "a second subcategory with the same name in one category, in any case, is refused" \
  "$(category $office $ana Oficina expense) $(subcategory $office Papelería) $(subcategory $office PAPELERÍA)"
allowed "the same subcategory name is allowed in another category" \
  "$(category $office $ana Oficina expense) $(category 00000000-0000-7000-8000-0000000000c2 $ana Viajes expense)
   $(subcategory $office Varios) $(subcategory 00000000-0000-7000-8000-0000000000c2 Varios)"
refused "a subcategory of a category that does not exist is refused" \
  "$(subcategory 00000000-0000-7000-8000-0000000000ff Papelería)"
refused "a category that owns a subcategory cannot be deleted (BR-05)" \
  "$(category $office $ana Oficina expense) $(subcategory $office Papelería) DELETE FROM transactions.categories WHERE id = '$office';"
allowed "a category without subcategories can be deleted" \
  "$(category $office $ana Oficina expense) DELETE FROM transactions.categories WHERE id = '$office';"

# Transactions003_CreateTransactions and Transactions004_CreateTransactionDetails (HU-04 to HU-06).
paper=00000000-0000-7000-8000-0000000000d1
movement=00000000-0000-7000-8000-0000000000e1
setup="$(category $office $ana Oficina expense)
  INSERT INTO transactions.subcategories (id, category_id, name, created_by, updated_by) VALUES ('$paper', '$office', 'Papelería', '$ana', '$ana');"
transaction() { # id description
  echo "INSERT INTO transactions.transactions (id, owned_by, transaction_date, description, created_by, updated_by) VALUES ('$1', '$ana', DATE '2026-11-06', '$2', '$ana', '$ana');"
}
detail() { # transaction subcategory amount [line_number, 1 by default]
  echo "INSERT INTO transactions.transaction_details (id, transaction_id, subcategory_id, amount, line_number, created_by, updated_by) VALUES (gen_random_uuid(), '$1', '$2', $3, ${4:-1}, '$ana', '$ana');"
}
check "transactions.transactions and .transaction_details exist" \
  "SELECT to_regclass('transactions.transactions') IS NOT NULL AND to_regclass('transactions.transaction_details') IS NOT NULL"
check "app role may read and write both, and nothing more" \
  "SELECT has_table_privilege('bud_tack_app', 'transactions.transactions', 'SELECT, INSERT, UPDATE, DELETE')
      AND has_table_privilege('bud_tack_app', 'transactions.transaction_details', 'SELECT, INSERT, UPDATE, DELETE')
      AND NOT has_table_privilege('bud_tack_app', 'transactions.transaction_details', 'TRUNCATE')"
check "both keys of a detail are ON DELETE RESTRICT" \
  "SELECT bool_and(confdeltype = 'r') AND count(*) = 2 FROM pg_constraint
    WHERE conname IN ('fk_transaction_details_transaction_id', 'fk_transaction_details_subcategory_id')"
check "a detail has no type column: it comes from its subcategory's category (BR-01)" \
  "SELECT NOT EXISTS (SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'transactions' AND table_name = 'transaction_details' AND column_name = 'type')"
allowed "a transaction with two lines is recorded, amounts kept to the cent" \
  "$setup $(transaction $movement 'Printer paper') $(detail $movement $paper 85.50 1) $(detail $movement $paper 0.01 2)
   DO \$\$ BEGIN
     IF (SELECT sum(amount) FROM transactions.transaction_details) <> 85.51 THEN RAISE EXCEPTION 'sum'; END IF;
   END \$\$;"
refused "an amount of zero is refused" \
  "$setup $(transaction $movement 'x') $(detail $movement $paper 0)"
refused "a negative amount is refused" \
  "$setup $(transaction $movement 'x') $(detail $movement $paper -1)"
refused "an amount of a trillion or more is refused" \
  "$setup $(transaction $movement 'x') $(detail $movement $paper 1000000000000.00)"
refused "a blank description is refused" \
  "$(transaction $movement '   ')"
refused "a description over 200 characters is refused" \
  "$(transaction $movement "$(printf 'a%.0s' $(seq 201))")"
refused "deleted_at without deleted_by is refused (BR-04)" \
  "$(transaction $movement 'x') UPDATE transactions.transactions SET deleted_at = now() WHERE id = '$movement';"
allowed "a soft delete sets deleted_at and deleted_by together (BR-04)" \
  "$(transaction $movement 'x') UPDATE transactions.transactions SET deleted_at = now(), deleted_by = '$ana' WHERE id = '$movement';"
refused "a detail against a subcategory that does not exist is refused" \
  "$(transaction $movement 'x') $(detail $movement 00000000-0000-7000-8000-0000000000ff 10)"
refused "a subcategory a detail uses cannot be deleted (BR-05)" \
  "$setup $(transaction $movement 'x') $(detail $movement $paper 10) DELETE FROM transactions.subcategories WHERE id = '$paper';"
refused "a transaction with lines cannot be physically deleted (BR-04)" \
  "$setup $(transaction $movement 'x') $(detail $movement $paper 10) DELETE FROM transactions.transactions WHERE id = '$movement';"

# Transactions005_AddDetailLineNumber: the lines keep the order they were entered.
numbered() { # line_number
  echo "INSERT INTO transactions.transaction_details (id, transaction_id, subcategory_id, amount, line_number, created_by, updated_by) VALUES (gen_random_uuid(), '$movement', '$paper', 1, $1, '$ana', '$ana');"
}
refused "two lines of one transaction cannot share a line number" \
  "$setup $(transaction $movement 'x') $(numbered 1) $(numbered 1)"
refused "a line number below 1 is refused" \
  "$setup $(transaction $movement 'x') $(numbered 0)"
refused "a line without a number is refused" \
  "$setup $(transaction $movement 'x')
   INSERT INTO transactions.transaction_details (id, transaction_id, subcategory_id, amount, created_by, updated_by)
   VALUES (gen_random_uuid(), '$movement', '$paper', 1, '$ana', '$ana');"
# Transactions006_CreateBudgets (HU-07).
budget() { # amount start end
  echo "INSERT INTO transactions.budgets (id, owned_by, category_id, amount, period_start, period_end, created_by, updated_by) VALUES (gen_random_uuid(), '$ana', '$office', $1, DATE '$2', DATE '$3', '$ana', '$ana');"
}
office_only="$(category $office $ana Oficina expense)"
check "transactions.budgets exists, and the app role may read and write it" \
  "SELECT to_regclass('transactions.budgets') IS NOT NULL
      AND has_table_privilege('bud_tack_app', 'transactions.budgets', 'SELECT, INSERT, UPDATE, DELETE')
      AND NOT has_table_privilege('bud_tack_app', 'transactions.budgets', 'TRUNCATE')"
check "a budget's key to its category is ON DELETE RESTRICT (BR-05)" \
  "SELECT confdeltype = 'r' FROM pg_constraint WHERE conname = 'fk_budgets_category_id'"
allowed "a budget of 500.00 for November is stored" \
  "$office_only $(budget 500.00 2026-11-01 2026-11-30)"
refused "a period that ends before it starts is refused" \
  "$office_only $(budget 500 2026-11-30 2026-11-01)"
refused "a period that ends the day it starts is refused" \
  "$office_only $(budget 500 2026-11-01 2026-11-01)"
refused "an amount of zero is refused" \
  "$office_only $(budget 0 2026-11-01 2026-11-30)"
refused "an amount of a trillion or more is refused" \
  "$office_only $(budget 1000000000000.00 2026-11-01 2026-11-30)"
refused "a change that breaks the period is refused, keeping the row (BR-06)" \
  "$office_only $(budget 500 2026-11-01 2026-11-30) UPDATE transactions.budgets SET period_end = DATE '2026-10-15';"
allowed "a change of amount is kept, and a budget can be deleted physically (BR-06)" \
  "$office_only $(budget 500 2026-11-01 2026-11-30) UPDATE transactions.budgets SET amount = 650;
   DELETE FROM transactions.budgets;
   DO \$\$ BEGIN IF EXISTS (SELECT 1 FROM transactions.budgets) THEN RAISE EXCEPTION 'kept'; END IF; END \$\$;"
refused "a category that owns a budget cannot be deleted (BR-05)" \
  "$office_only $(budget 500 2026-11-01 2026-11-30) DELETE FROM transactions.categories WHERE id = '$office';"
allowed "once its budget is deleted, the category can be (TC-019)" \
  "$office_only $(budget 500 2026-11-01 2026-11-30) DELETE FROM transactions.budgets; DELETE FROM transactions.categories WHERE id = '$office';"
refused "a budget for a category that does not exist is refused" \
  "INSERT INTO transactions.budgets (id, owned_by, category_id, amount, period_start, period_end, created_by, updated_by)
   VALUES (gen_random_uuid(), '$ana', '00000000-0000-7000-8000-0000000000ff', 1, DATE '2026-11-01', DATE '2026-11-30', '$ana', '$ana');"

check "the line order index replaces the plain transaction_id index" \
  "SELECT to_regclass('transactions.idx_transaction_details_transaction_id_line_number') IS NOT NULL
      AND to_regclass('transactions.idx_transaction_details_transaction_id') IS NULL"

migrate down
check "down removes identity.users and every transactions table" \
  "SELECT to_regclass('identity.users') IS NULL AND to_regclass('transactions.categories') IS NULL
      AND to_regclass('transactions.subcategories') IS NULL AND to_regclass('transactions.transactions') IS NULL
      AND to_regclass('transactions.transaction_details') IS NULL AND to_regclass('transactions.budgets') IS NULL"
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
