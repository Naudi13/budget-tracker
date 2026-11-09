"""NFR-07 (11-quality/testing-strategy.md): every module keeps at least 80 % line
coverage, and BudTack.Transactions 90 %.

A module's coverage is the union of the cobertura reports given: unit tests
cover the domain and use cases, module-integration tests cover what only a real
PostgreSQL can prove (the repositories). A line counts once, covered if any
report ran it. The host (BudTack.Api) is wiring and HTTP glue, proven by the
end-to-end layer instead, so it is reported but not gated.

Usage: python3 scripts/coverage-gate.py unit.cobertura.xml module-integration.cobertura.xml
"""

import sys
import xml.etree.ElementTree as ElementTree

MINIMUM = {"BudTack.Transactions": 90.0}
DEFAULT_MINIMUM = 80.0
NOT_GATED = {"BudTack.Api"}


def lines_by_package(paths):
    packages = {}
    for path in paths:
        root = ElementTree.parse(path).getroot()
        for package in root.iter("package"):
            lines = packages.setdefault(package.get("name"), {})
            for cls in package.iter("class"):
                filename = cls.get("filename")
                for line in cls.iter("line"):
                    key = (filename, int(line.get("number")))
                    lines[key] = lines.get(key, False) or int(line.get("hits", "0")) > 0
    return packages


def main(paths):
    if not paths:
        print(__doc__)
        return 2

    failures = 0
    for name, lines in sorted(lines_by_package(paths).items()):
        if not lines:
            continue
        percent = 100.0 * sum(lines.values()) / len(lines)
        if name in NOT_GATED:
            print(f"info  {name} {percent:.1f} % (covered by end-to-end, not gated)")
            continue
        minimum = MINIMUM.get(name, DEFAULT_MINIMUM)
        verdict = "PASS" if percent >= minimum else "FAIL"
        failures += verdict == "FAIL"
        print(f"{verdict}  {name} {percent:.1f} % (minimum {minimum:.0f} %)")

    if failures:
        print(f"{failures} module(s) under the NFR-07 minimum.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
