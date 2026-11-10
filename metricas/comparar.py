"""Compara dos mediciones de docs/metricas y escribe docs/metricas/comparacion.md.

Uso, desde la raíz del repositorio:
    python metricas/comparar.py antes despues
"""
import csv
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BASE = ROOT / "docs" / "metricas"
WATCHED = [("MovementRules", "Validate")]
WATCHED_CLASSES = ["MovementRules", "SignIn", "ModuleErrorFilter"]


def rows(name, file):
    with open(BASE / name / "datos" / file, encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def number(value):
    return float(value) if value not in ("", None) else None


def summary(name):
    methods = rows(name, "vg_metodos.csv")
    classes = [c for c in rows(name, "ck_clases.csv") if c["proyecto"] != "BudTack.Db.Migrations"]
    with_mi = [float(c["mi"]) for c in classes if number(c["mi"]) is not None]
    maintenance = {r["tipo"]: r for r in rows(name, "mantenimiento.csv")}
    login = [r["estado"] for r in rows(name, "login.csv")]
    duplication = {r["area"]: r["pct_duplicado"] for r in rows(name, "duplicacion.csv")}
    over = lambda key, limit: sum(number(c[key]) is not None and number(c[key]) > limit for c in classes)
    return {
        "Métodos medidos": len(methods),
        "Métodos con V(G) > 10": sum(int(m["vg"]) > 10 for m in methods),
        "V(G) máximo": max(int(m["vg"]) for m in methods),
        "Métodos con más de 50 líneas": sum(int(m["nloc"]) > 50 for m in methods),
        "Clases de la aplicación": len(classes),
        "Clases con WMC > 20": over("wmc", 20),
        "Clases con CBO propio > 10": over("cbo_propio", 10),
        "Clases con LCOM* > 0.7": over("lcom", 0.7),
        "Clases con MI < 65": sum(mi < 65 for mi in with_mi),
        "MI promedio": round(sum(with_mi) / len(with_mi), 2),
        "Duplicación en la API (%)": duplication.get("api", ""),
        "Mantenimiento preventivo (% de commits)": maintenance["Preventivo"]["pct_commits"],
        "Inicio de sesión: 6 intentos fallidos": ", ".join(login) if login else "No medido",
    }


def method_vg(name, cls, method):
    for row in rows(name, "vg_roslyn.csv"):
        if row["clase"] == cls and row["metodo"] == method:
            return row["vg"]
    return "—"


def class_row(name, cls):
    for row in rows(name, "ck_clases.csv"):
        if row["clase"] == cls:
            return row
    return None


def table(header, body):
    lines = ["| " + " | ".join(header) + " |", "|" + "---|" * len(header)]
    lines += ["| " + " | ".join(str(cell) for cell in row) + " |" for row in body]
    return "\n".join(lines)


def main():
    if len(sys.argv) != 3:
        sys.exit("Uso: python metricas/comparar.py <antes> <despues>")
    before, after = sys.argv[1], sys.argv[2]
    a, b = summary(before), summary(after)
    elements = [(f"{cls}.{method}", "V(G)", method_vg(before, cls, method), method_vg(after, cls, method)) for cls, method in WATCHED]
    for cls in WATCHED_CLASSES:
        x, y = class_row(before, cls), class_row(after, cls)
        for key, label in (("wmc", "WMC"), ("cbo", "CBO"), ("mi", "MI")):
            elements.append((cls, label, x[key] if x else "—", y[key] if y else "—"))

    parts = [
        "# Comparación antes y después",
        f"Compara [`{before}`](./{before}/resultados.md) con [`{after}`](./{after}/resultados.md). "
        "Generado por `metricas/comparar.py`; las definiciones están en [`fichas.md`](./fichas.md).",
        "## Resumen",
        table(["Indicador", "Antes", "Después"], [(k, a[k], b[k]) for k in a]),
        "## Elementos modificados",
        table(["Elemento", "Métrica", "Antes", "Después"], elements),
    ]
    (BASE / "comparacion.md").write_text("\n\n".join(parts) + "\n", encoding="utf-8", newline="\n")
    print("Listo: docs/metricas/comparacion.md")


if __name__ == "__main__":
    main()
