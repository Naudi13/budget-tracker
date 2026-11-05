#!/bin/sh
# NFR-07 on the unit layer (11-quality/testing-strategy.md): every module
# assembly keeps at least 80 % line coverage, and BudTack.Transactions 90 %.
# The host (BudTack.Api) is wiring and HTTP glue, proven by the end-to-end
# layer instead, so it is reported but not gated.
#
# Usage: sh scripts/coverage-gate.sh path/to/unit.cobertura.xml
set -eu

FILE="${1:?usage: coverage-gate.sh <cobertura.xml>}"
[ -f "$FILE" ] || { echo "no coverage file at $FILE" >&2; exit 1; }

failures=0
for package in $(grep -oE '<package [^>]*name="[^"]+"' "$FILE" | grep -oE 'name="[^"]+"' | cut -d'"' -f2); do
  rate=$(grep -oE "<package line-rate=\"[0-9.]+\"[^>]*name=\"$package\"" "$FILE" | grep -oE 'line-rate="[0-9.]+"' | cut -d'"' -f2)
  percent=$(awk "BEGIN { printf \"%.1f\", $rate * 100 }")
  case "$package" in
    BudTack.Transactions) minimum=90 ;;
    BudTack.Api) echo "info  $package $percent % (covered by end-to-end, not gated)"; continue ;;
    *) minimum=80 ;;
  esac
  if awk "BEGIN { exit !($rate * 100 >= $minimum) }"; then
    echo "PASS  $package $percent % (minimum $minimum %)"
  else
    echo "FAIL  $package $percent % (minimum $minimum %)"
    failures=$((failures + 1))
  fi
done

[ "$failures" -eq 0 ] || { echo "$failures module(s) under the NFR-07 minimum." >&2; exit 1; }
