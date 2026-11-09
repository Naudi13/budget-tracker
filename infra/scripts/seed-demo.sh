#!/bin/sh
# Fills the running application with one demo business, through the API and
# the portal's one door (ADR-006), so a demo never starts on empty screens.
# Run after `docker compose up -d --build --wait`; needs curl.
#
# Usage: sh scripts/seed-demo.sh         (from the repository root, .env present)
#
# Creates the account DEMO_EMAIL / DEMO_PASSWORD (defaults below, local only),
# then its categories, subcategories, 24 transactions from September to
# November 2026, and budgets. Run it once: if the account exists it stops
# without touching anything. To start over: `docker compose down
# --volumes`, then up again.
set -eu

[ -f .env ] || { echo "missing .env — copy .env.example to .env" >&2; exit 1; }
set -a; . ./.env; set +a

BASE="http://localhost:${PORTAL_PORT:-8080}/api/v1"
DEMO_NAME="${DEMO_NAME:-Papelería El Estudiante}"
DEMO_EMAIL="${DEMO_EMAIL:-demo@budgettracker.test}"
DEMO_PASSWORD="${DEMO_PASSWORD:-Demo-2026!}"
TOKEN=""

# send METHOD PATH EXPECTED_STATUS — the JSON body comes on stdin, so accented
# text reaches curl as UTF-8 bytes on every shell (Git Bash included).
# Prints the response body; stops the script on any other status.
send() {
  response=$(curl -s -w '\n%{http_code}' -X "$1" "$BASE$2" \
    -H 'Content-Type: application/json' ${TOKEN:+-H "Authorization: Bearer $TOKEN"} \
    --data-binary @-) || { echo "no answer from $BASE — is the stack up?" >&2; exit 1; }
  status=$(printf '%s' "$response" | tail -n 1)
  body=$(printf '%s' "$response" | sed '$d')
  if [ "$status" != "$3" ]; then
    echo "$1 $2: status $status, expected $3" >&2
    echo "$body" >&2
    exit 1
  fi
  printf '%s' "$body"
}

# id_of BODY — the first "id" of a response: the resource's own (its contract
# lists it first). Stops the script when there is none.
id_of() {
  id=$(printf '%s' "$1" | sed -n 's/^{"id":"\([^"]*\)".*/\1/p')
  [ -n "$id" ] || { echo "no id in: $1" >&2; exit 1; }
  printf '%s' "$id"
}

category() {
  body=$(printf '{"name":"%s","type":"%s"}' "$1" "$2" | send POST /categories 201) || exit 1
  id_of "$body"
}

subcategory() {
  body=$(printf '{"name":"%s"}' "$2" | send POST "/categories/$1/subcategories" 201) || exit 1
  id_of "$body"
}

# transaction DATE DESCRIPTION SUBCATEGORY_ID AMOUNT [SUBCATEGORY_ID AMOUNT ...]
transaction() {
  date=$1; description=$2; shift 2
  details=""
  while [ "$#" -ge 2 ]; do
    details="$details${details:+,}{\"subcategoryId\":\"$1\",\"amount\":$2}"
    shift 2
  done
  printf '{"transactionDate":"%s","description":"%s","details":[%s]}' "$date" "$description" "$details" \
    | send POST /transactions 201 >/dev/null
}

# budget CATEGORY_ID AMOUNT FROM TO
budget() {
  printf '{"categoryId":"%s","amount":%s,"periodStart":"%s","periodEnd":"%s"}' "$1" "$2" "$3" "$4" \
    | send POST /budgets 201 >/dev/null
}

# --- The account ---------------------------------------------------------------

signup=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/users" -H 'Content-Type: application/json' \
  --data-binary @- <<EOF
{"name":"$DEMO_NAME","email":"$DEMO_EMAIL","password":"$DEMO_PASSWORD"}
EOF
) || { echo "no answer from $BASE — is the stack up?" >&2; exit 1; }
case "$signup" in
  201) ;;
  409) echo "$DEMO_EMAIL already exists: nothing seeded. Start over with 'docker compose down --volumes'."; exit 0 ;;
  *) echo "POST /users: status $signup, expected 201" >&2; exit 1 ;;
esac

session=$(printf '{"email":"%s","password":"%s"}' "$DEMO_EMAIL" "$DEMO_PASSWORD" | send POST /sessions 201)
TOKEN=$(printf '%s' "$session" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
[ -n "$TOKEN" ] || { echo "no token in: $session" >&2; exit 1; }

# --- Categories and subcategories (HU-02) --------------------------------------

ventas=$(category "Ventas" income)
mostrador=$(subcategory "$ventas" "Mostrador")
domicilios=$(subcategory "$ventas" "Domicilios")
servicios=$(category "Servicios" income)
fotocopias=$(subcategory "$servicios" "Fotocopias e impresión")
plastificado=$(subcategory "$servicios" "Plastificado")

inventario=$(category "Inventario" expense)
cuadernos=$(subcategory "$inventario" "Cuadernos")
utiles=$(subcategory "$inventario" "Útiles escolares")
arriendo=$(category "Arriendo" expense)
local_=$(subcategory "$arriendo" "Local")
publicos=$(category "Servicios públicos" expense)
energia=$(subcategory "$publicos" "Energía")
internet=$(subcategory "$publicos" "Internet")
nomina=$(category "Nómina" expense)
salarios=$(subcategory "$nomina" "Salarios")
oficina=$(category "Oficina" expense)
aseo=$(subcategory "$oficina" "Aseo")
papeleria=$(subcategory "$oficina" "Papelería interna")
publicidad=$(category "Publicidad" expense)
volantes=$(subcategory "$publicidad" "Volantes")

# --- Transactions (HU-04), some with several lines -----------------------------

transaction 2026-09-02 "Arriendo de septiembre"            "$local_" 1800000
transaction 2026-09-05 "Surtido de regreso a clases"       "$cuadernos" 950000 "$utiles" 420000
transaction 2026-09-08 "Ventas de la semana"               "$mostrador" 3350000
transaction 2026-09-12 "Volantes de temporada escolar"     "$volantes" 180000
transaction 2026-09-15 "Servicios de la quincena"          "$fotocopias" 320000 "$plastificado" 85000
transaction 2026-09-20 "Factura de energía"                "$energia" 210500
transaction 2026-09-22 "Plan de internet"                  "$internet" 95000
transaction 2026-09-27 "Ventas de fin de mes"              "$mostrador" 3620000 "$domicilios" 540000
transaction 2026-09-30 "Salario del auxiliar"              "$salarios" 1300000

transaction 2026-10-01 "Arriendo de octubre"               "$local_" 1800000
transaction 2026-10-06 "Ventas de la semana"               "$mostrador" 3480000
transaction 2026-10-10 "Reposición de útiles"              "$utiles" 610000
transaction 2026-10-14 "Fotocopias de la quincena"         "$fotocopias" 290000
transaction 2026-10-18 "Factura de energía"                "$energia" 198750
transaction 2026-10-20 "Plan de internet"                  "$internet" 95000
transaction 2026-10-25 "Ventas de fin de mes"              "$mostrador" 3910000 "$domicilios" 610000
transaction 2026-10-28 "Productos de aseo"                 "$aseo" 64900
transaction 2026-10-31 "Salario del auxiliar"              "$salarios" 1300000

transaction 2026-11-01 "Arriendo de noviembre"             "$local_" 1800000
transaction 2026-11-02 "Pedidos a domicilio"               "$domicilios" 185000
transaction 2026-11-03 "Ventas de la semana"               "$mostrador" 2950000
transaction 2026-11-04 "Pedido de cuadernos"               "$cuadernos" 780000
transaction 2026-11-05 "Fotocopias e impresiones"          "$fotocopias" 265500.50
transaction 2026-11-06 "Aseo y papelería de la oficina"    "$aseo" 48300 "$papeleria" 35200

# --- A category no longer used (HU-03, BR-05): kept in the history, not offered.

printf '{"isActive":false}' | send PATCH "/categories/$publicidad" 200 >/dev/null

# --- Budgets (HU-07) -------------------------------------------------------------

budget "$inventario" 1200000 2026-10-01 2026-10-31
budget "$publicos"    350000 2026-10-01 2026-10-31
budget "$inventario" 1000000 2026-11-01 2026-11-30
budget "$arriendo"   1800000 2026-11-01 2026-11-30
budget "$publicos"    320000 2026-11-01 2026-11-30
budget "$nomina"     1300000 2026-11-01 2026-11-30
budget "$oficina"     150000 2026-11-01 2026-11-30
budget "$inventario" 1500000 2026-12-01 2026-12-31

echo "Demo data ready. Sign in at http://localhost:${PORTAL_PORT:-8080} as $DEMO_EMAIL."
