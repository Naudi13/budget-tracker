#!/bin/sh
# Runs once, on the container's first start with an empty volume
# (docker-entrypoint-initdb.d). It creates the two roles that
# 00-governance/security-policy.md requires:
#   bud_tack_migrator — owns the database; bud-tack-db runs every DDL as it
#   bud_tack_app      — the API's role; may connect, never create or drop
# Schemas (identity, transactions) and their grants are NOT created here:
# they are the first migration in bud-tack-db (ADR-005).
set -eu

psql -v ON_ERROR_STOP=1 \
     --username "$POSTGRES_USER" \
     --dbname "$POSTGRES_DB" \
     -v db="$POSTGRES_DB" \
     -v migrator_password="$BUD_TACK_MIGRATOR_PASSWORD" \
     -v app_password="$BUD_TACK_APP_PASSWORD" <<'EOSQL'
CREATE ROLE bud_tack_migrator
  LOGIN PASSWORD :'migrator_password'
  NOSUPERUSER NOCREATEDB NOCREATEROLE;

CREATE ROLE bud_tack_app
  LOGIN PASSWORD :'app_password'
  NOSUPERUSER NOCREATEDB NOCREATEROLE;

-- The migrator owns the database, so it can create schemas in it. Since
-- PostgreSQL 15 the database owner also owns the `public` schema, where
-- FluentMigrator keeps public.schema_version.
ALTER DATABASE :"db" OWNER TO bud_tack_migrator;

-- Nobody but these two roles connects; the app gets CONNECT and nothing else.
REVOKE ALL ON DATABASE :"db" FROM PUBLIC;
GRANT CONNECT ON DATABASE :"db" TO bud_tack_app;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
EOSQL
