#!/bin/sh
# Prints a password reset link for an administrator, straight from the database:
# the way back when the only administrator forgot the password (ADR-010).
# Needs the application running, and access to this machine's Docker.
#
# Usage: sh scripts/admin-reset-link.sh admin@budgettracker.test      (from the repository root)
set -eu

[ -f .env ] || { echo "missing .env — copy .env.example to .env" >&2; exit 1; }
set -a; . ./.env; set +a
[ "$#" -eq 1 ] || { echo "usage: sh scripts/admin-reset-link.sh EMAIL" >&2; exit 1; }

token=$(docker compose exec -T postgres psql -U "${POSTGRES_SUPERUSER:-postgres}" -d "${POSTGRES_DB:-app_dev}" \
  -v ON_ERROR_STOP=1 -X -q -t -A -v email="$1" <<'EOSQL'
WITH account AS (
  SELECT id FROM identity.users WHERE email = :'email' AND role = 'ADMIN'
), generated AS (
  SELECT replace(gen_random_uuid()::text, '-', '') || replace(gen_random_uuid()::text, '-', '') AS token
), cancelled AS (
  UPDATE identity.account_tokens SET used_at = now()
  WHERE user_id IN (SELECT id FROM account) AND used_at IS NULL
), stored AS (
  INSERT INTO identity.account_tokens (id, user_id, purpose, token_hash, expires_at, created_by)
  SELECT gen_random_uuid(), account.id, 'PASSWORD_RESET',
         encode(sha256(convert_to(generated.token, 'UTF8')), 'hex'), now() + interval '30 minutes', account.id
  FROM account, generated
  RETURNING id
)
SELECT generated.token FROM generated, stored;
EOSQL
)

[ -n "$token" ] || { echo "no administrator has the email $1" >&2; exit 1; }
echo "http://localhost:${PORTAL_PORT:-8080}/reset-password#token=$token"
echo "It works once, for 30 minutes."
