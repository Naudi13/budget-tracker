#!/bin/sh
# Proves the whole application answers through its one door, the portal
# (ADR-006), after `docker compose up -d --build --wait`.
# Used by CI and by a developer; needs curl on the host.
#
# Usage: sh scripts/verify-stack.sh      (from the repository root, .env present)
set -eu

[ -f .env ] || { echo "missing .env — copy .env.example to .env" >&2; exit 1; }
set -a; . ./.env; set +a

BASE="http://localhost:${PORTAL_PORT:-8080}"
failures=0

pass() { echo "PASS  $1"; }
fail() { echo "FAIL  $1"; failures=$((failures + 1)); }

# check NAME URL EXPECTED_STATUS [TEXT_IN_BODY]
check() {
  body=$(curl -s -o - -w '\n%{http_code}' "$2") || { fail "$1: no answer"; return; }
  status=$(printf '%s' "$body" | tail -n 1)
  if [ "$status" != "$3" ]; then fail "$1: status $status, expected $3"; return; fi
  if [ -n "${4:-}" ] && ! printf '%s' "$body" | grep -q "$4"; then fail "$1: body lacks $4"; return; fi
  pass "$1"
}

check "portal serves the Angular app at /"              "$BASE/"                      200 "<bt-root"
check "a deep link is answered by the Angular app"     "$BASE/transactions/history"  200 "<bt-root"
check "/health reaches the API and its database"       "$BASE/health"                200 '"database":"Healthy"'
check "/api/ reaches the API and its error format"     "$BASE/api/v1/no-such-thing"  404 '"error":"NOT_FOUND"'

check "Swagger UI loads through the portal"             "$BASE/swagger/index.html"    200 "swagger"
check "Swagger documents the API"                      "$BASE/swagger/v1/swagger.json" 200 "/api/v1/users"
check "a protected endpoint refuses no token"          "$BASE/api/v1/users"          401 '"error":"UNAUTHORIZED"'

# token EMAIL PASSWORD — the bearer token of a seed user (Identity003_SeedUsers).
token() {
  printf '{"email":"%s","password":"%s"}' "$1" "$2" \
    | curl -s -X POST "$BASE/api/v1/sessions" -H 'Content-Type: application/json' --data-binary @- \
    | sed -n 's/.*"token":"\([^"]*\)".*/\1/p'
}

# status_with TOKEN — what GET /users answers to that token.
status_with() {
  curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $1" "$BASE/api/v1/users"
}

admin=$(token admin@budgettracker.test 'Admin-2026!')
user=$(token usuario@budgettracker.test 'Usuario-2026!')
signup=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/v1/users" -H 'Content-Type: application/json' \
  -d '{"name":"Eve","email":"eve@example.com","role":"ADMIN"}' || true)
if [ "$signup" = "401" ]; then pass "nobody signs up without an administrator"; else fail "nobody signs up without an administrator (status $signup)"; fi

if [ -n "$admin" ] && [ "$(status_with "$admin")" = "200" ]; then pass "the seeded ADMIN lists the accounts"; else fail "the seeded ADMIN lists the accounts"; fi
if [ -n "$user" ] && [ "$(status_with "$user")" = "403" ]; then pass "the seeded USER gets 403"; else fail "the seeded USER gets 403"; fi

if curl -s -o /dev/null --max-time 3 "http://localhost:3000/health"; then
  fail "the API is not published on the host (ADR-006)"
else
  pass "the API is not published on the host (ADR-006)"
fi

if [ "$(docker compose ps -a migrate --format '{{.ExitCode}}')" = "0" ]; then
  pass "migrations ran and exited cleanly"
else
  fail "migrations ran and exited cleanly"
fi

schemas=$(docker compose exec -T postgres psql -U "${POSTGRES_SUPERUSER:-postgres}" -d "${POSTGRES_DB:-app_dev}" \
  -X -q -t -A -c "SELECT string_agg(nspname, ',' ORDER BY nspname) FROM pg_namespace WHERE nspname IN ('identity','transactions')")
if [ "$schemas" = "identity,transactions" ]; then pass "the module schemas exist"; else fail "the module schemas exist (found: $schemas)"; fi

echo
if [ "$failures" -eq 0 ]; then echo "All checks passed."; else echo "$failures check(s) failed."; exit 1; fi
