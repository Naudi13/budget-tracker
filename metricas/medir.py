"""Mide el código y escribe docs/metricas/<nombre>/resultados.md y sus CSV.

Uso, desde la raíz del repositorio:
    python metricas/medir.py --nombre antes [--base-url http://localhost:8080] [--no-build]

--nombre separa cada medición, por ejemplo "antes" y "despues", para compararlas.
--base-url prueba además el límite de intentos de inicio de sesión contra la aplicación en marcha.
Las definiciones y umbrales de cada métrica están en docs/metricas/fichas.md.
"""
import argparse
import csv
import io
import json
import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.request
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "docs" / "metricas" / "actual" / "datos"
REPORT = ROOT / "docs" / "metricas" / "actual" / "resultados.md"

AREAS = [
    ("api", "csharp", "src/api/src", ".cs"),
    ("db", "csharp", "src/db/src", ".cs"),
    ("portal", "typescript", "src/portal/src/app", ".ts"),
]
SKIP_DIRS = {"bin", "obj", "node_modules"}
LIZARD_EXCLUDE = ["-x", "*/obj/*", "-x", "*/bin/*", "-x", "*.spec.ts"]
MAINTENANCE = {
    "fix": "Correctivo",
    "chore": "Adaptativo",
    "build": "Adaptativo",
    "ci": "Adaptativo",
    "feat": "Perfectivo",
    "refactor": "Preventivo",
}
CLASSIC = {"Correctivo": 21, "Adaptativo": 25, "Perfectivo": 50, "Preventivo": 4}
LOCK_FILES = ("package-lock.json", ".png")
ACTIONS = {
    "V(G)": "Extract Method",
    "Líneas por método": "Extract Method",
    "WMC": "Extract Class o Extract Method",
    "CBO": "Inyección de dependencias o Facade",
    "RFC": "Extract Class",
    "DIT": "Composición en lugar de herencia",
    "LCOM*": "Extract Class",
    "MI": "Extract Method y reducir tamaño",
}


def run(args, cwd=ROOT, check=True):
    result = subprocess.run(args, cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and result.returncode != 0:
        sys.exit(f"Falló {' '.join(map(str, args))}:\n{result.stdout}\n{result.stderr}")
    return result.stdout


def write_csv(name, header, rows):
    with open(OUT / name, "w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, lineterminator="\n")
        writer.writerow(header)
        writer.writerows(rows)


def clean_name(long_name):
    return long_name.split("(")[0].strip()


def measure_complexity():
    methods = []
    for area, language, path, _ in AREAS:
        output = run([sys.executable, "-m", "lizard", "-l", language, path, *LIZARD_EXCLUDE, "--csv"])
        for row in csv.reader(io.StringIO(output)):
            if row[7] == "*global*":
                continue
            methods.append({
                "area": area,
                "metodo": clean_name(row[8]),
                "archivo": row[6].replace("\\", "/"),
                "linea": int(row[9]),
                "vg": int(row[1]),
                "nloc": int(row[0]),
            })
    methods.sort(key=lambda m: (m["area"], m["archivo"], m["linea"]))
    write_csv("vg_metodos.csv", ["area", "metodo", "archivo", "linea", "vg", "nloc"],
              [[m[k] for k in ("area", "metodo", "archivo", "linea", "vg", "nloc")] for m in methods])
    return methods


def count_lines(path):
    effective = comments = blank = 0
    in_block = False
    for raw in path.read_text(encoding="utf-8", errors="ignore").splitlines():
        line = raw.strip()
        if not line:
            blank += 1
        elif in_block:
            comments += 1
            in_block = "*/" not in line
        elif line.startswith("//"):
            comments += 1
        elif line.startswith("/*"):
            comments += 1
            in_block = "*/" not in line
        else:
            effective += 1
    return effective, comments, blank


def measure_loc():
    rows = []
    for area, _, path, extension in AREAS:
        files = effective = comments = blank = 0
        for folder, dirs, names in os.walk(ROOT / path):
            dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
            for name in names:
                if name.endswith(extension) and not name.endswith(".spec.ts"):
                    e, c, b = count_lines(Path(folder) / name)
                    files += 1
                    effective += e
                    comments += c
                    blank += b
        rows.append([area, files, effective + comments + blank, effective, comments, blank,
                     round(100 * comments / (effective + comments), 1)])
    write_csv("loc.csv", ["area", "archivos", "fisicas", "efectivas", "comentarios", "en_blanco", "pct_comentarios"], rows)
    return rows


def measure_duplication():
    rows = []
    for area, language, path, _ in AREAS:
        # Con esta extensión, lizard termina con código distinto de cero cuando encuentra duplicados.
        output = run([sys.executable, "-m", "lizard", "-l", language, "-Eduplicate", path, *LIZARD_EXCLUDE, "-x", "*/Migrations/*"], check=False)
        match = re.search(r"Total duplicate rate: ([\d.]+)%", output)
        if not match:
            sys.exit(f"lizard no informó la duplicación de {area}:\n{output}")
        rows.append([area, float(match.group(1))])
    write_csv("duplicacion.csv", ["area", "pct_duplicado"], rows)
    return rows


def measure_design(build):
    if build:
        run(["dotnet", "build", "-c", "Release", "-v:q", "-nologo"], ROOT / "src/api")
        run(["dotnet", "build", "-c", "Release", "-v:q", "-nologo"], ROOT / "src/db")
    run(["dotnet", "run", "--project", "metricas/BudTack.Metrics", "-c", "Release", "--", str(ROOT), str(OUT)])
    with open(OUT / "ck_clases.csv", encoding="utf-8", newline="") as handle:
        classes = list(csv.DictReader(handle))
    with open(OUT / "vg_roslyn.csv", encoding="utf-8", newline="") as handle:
        roslyn = list(csv.DictReader(handle))
    return classes, roslyn


def measure_maintenance():
    output = run(["git", "log", "--no-merges", "--numstat", "--format=%x00%s", "HEAD"])
    totals = defaultdict(lambda: [0, 0])
    for entry in output.split("\x00")[1:]:
        lines = entry.strip("\n").split("\n")
        match = re.match(r"^([a-z]+)(\(.*?\))?!?:", lines[0])
        kind = MAINTENANCE.get(match.group(1), "Fuera de la clasificación") if match else "Fuera de la clasificación"
        totals[kind][0] += 1
        for line in lines[1:]:
            parts = line.split("\t")
            if len(parts) == 3 and parts[0].isdigit() and parts[1].isdigit() and not parts[2].endswith(LOCK_FILES):
                totals[kind][1] += int(parts[0]) + int(parts[1])
    maintained = sum(totals[k][0] for k in CLASSIC)
    rows = []
    for kind, classic in CLASSIC.items():
        commits, churn = totals[kind]
        rows.append([kind, commits, churn, round(100 * commits / maintained, 1) if maintained else 0.0, classic])
    write_csv("mantenimiento.csv", ["tipo", "commits", "lineas_cambiadas", "pct_commits", "pct_clasico"], rows)
    return rows, totals["Fuera de la clasificación"]


def check_login(base_url):
    email = f"medicion-{int(time.time())}@example.com"
    body = json.dumps({"email": email, "password": "contrasena-incorrecta"}).encode()
    statuses = []
    for _ in range(6):
        request = urllib.request.Request(f"{base_url}/api/v1/sessions", data=body, headers={"Content-Type": "application/json"}, method="POST")
        try:
            with urllib.request.urlopen(request) as response:
                statuses.append(response.status)
        except urllib.error.HTTPError as error:
            statuses.append(error.code)
    write_csv("login.csv", ["intento", "estado"], [[i + 1, s] for i, s in enumerate(statuses)])


def read_login():
    path = OUT / "login.csv"
    if not path.exists():
        return None
    with open(path, encoding="utf-8", newline="") as handle:
        return [int(row["estado"]) for row in csv.DictReader(handle)]


def number(value):
    return float(value) if value not in ("", None) else None


def exceedances(methods, classes):
    rows = []
    for m in methods:
        if m["vg"] > 10:
            band = "Moderado" if m["vg"] <= 20 else "Alto" if m["vg"] <= 50 else "Muy alto"
            rows.append((m["metodo"], "V(G)", m["vg"], "≤ 10", band))
        if m["nloc"] > 50:
            rows.append((m["metodo"], "Líneas por método", m["nloc"], "≤ 50", "Alerta"))
    limits = [("wmc", "WMC", 20), ("dit", "DIT", 5), ("cbo", "CBO", 10), ("rfc", "RFC", 50), ("lcom", "LCOM*", 0.7)]
    for c in classes:
        for key, label, limit in limits:
            value = number(c[key])
            if value is not None and value > limit:
                rows.append((c["clase"], label, c[key], f"≤ {limit}", "Supera"))
        mi = number(c["mi"])
        if mi is not None and mi < 65:
            rows.append((c["clase"], "MI", c["mi"], "≥ 65", "Baja"))
    return rows


def action(row):
    if re.match(r"^[A-Za-z]+\d{3}_", str(row[0])):
        return "Ninguna: una migración aplicada no se modifica"
    return ACTIONS[row[1]]


def table(header, rows):
    lines = ["| " + " | ".join(header) + " |", "|" + "---|" * len(header)]
    lines += ["| " + " | ".join(str(cell) for cell in row) + " |" for row in rows]
    return "\n".join(lines)


def vg_bands(methods, area):
    selected = [m for m in methods if m["area"] == area]
    count = lambda low, high: sum(low <= m["vg"] <= high for m in selected)
    return [area, len(selected), count(1, 10), count(11, 20), count(21, 50), count(51, 10**9),
            round(sum(m["vg"] for m in selected) / len(selected), 2), max(m["vg"] for m in selected)]


def write_report(methods, loc, duplication, classes, roslyn, maintenance, unclassified, login):
    commit = run(["git", "rev-parse", "--short", "HEAD"]).strip()
    measured = [c for c in classes if number(c["mi"]) is not None]
    lcom = [number(c["lcom"]) for c in classes if number(c["lcom"]) is not None]
    roslyn_over = sorted((r for r in roslyn if int(r["vg"]) > 10), key=lambda r: -int(r["vg"]))
    average = lambda values: round(sum(values) / len(values), 2)
    worst_wmc = sorted(classes, key=lambda c: -int(c["wmc"]))[:10]
    worst_cbo = sorted(classes, key=lambda c: -int(c["cbo"]))[:10]
    worst_mi = sorted(measured, key=lambda c: float(c["mi"]))[:10]
    design = lambda c: [c["clase"], c["wmc"], c["dit"], c["noc"], c["cbo"], c["cbo_propio"], c["rfc"], c["lcom"] or "n/a", c["mi"] or "n/a"]
    design_header = ["Clase", "WMC", "DIT", "NOC", "CBO", "CBO propio", "RFC", "LCOM*", "MI"]
    found = exceedances(methods, classes)

    parts = [
        "# Resultados de las métricas",
        f"Generado por `metricas/medir.py` sobre el commit `{commit}`. Las definiciones están en [`fichas.md`](../fichas.md) "
        "y los datos completos en [`datos/`](./datos).",
        "## Complejidad ciclomática V(G)",
        table(["Parte", "Métodos", "1–10", "11–20", "21–50", "> 50", "Promedio", "Máximo"], [vg_bands(methods, a[0]) for a in AREAS]),
        "## Líneas de código",
        table(["Parte", "Archivos", "Físicas", "Efectivas", "Comentarios", "En blanco", "% comentarios"], loc),
        f"Métodos con más de 50 líneas efectivas: {sum(m['nloc'] > 50 for m in methods)}.",
        "## Diseño: suite CK e Índice de Mantenibilidad",
        f"{len(classes)} clases de C# medidas. MI promedio: {average([float(c['mi']) for c in measured])} "
        f"(mínimo {min(float(c['mi']) for c in measured)}), sobre {len(measured)} clases con métodos. "
        f"LCOM* calculable en {len(lcom)} clases.",
        "### Clases con mayor WMC",
        table(design_header, [design(c) for c in worst_wmc]),
        "### Clases con mayor CBO",
        table(design_header, [design(c) for c in worst_cbo]),
        "### Clases con menor MI",
        table(design_header, [design(c) for c in worst_mi]),
        "## Costo de mantenimiento",
        table(["Tipo", "Commits", "Líneas cambiadas", "% de commits", "% clásico"], maintenance),
        f"Fuera de la clasificación: {unclassified[0]} commits (documentación, pruebas y mensajes sin tipo).",
        "## Duplicación (sin migraciones)",
        table(["Parte", "% duplicado"], duplication),
        "## Límite de intentos de inicio de sesión (RNF-02)",
        (f"Respuestas a 6 intentos fallidos seguidos con un mismo correo: {', '.join(map(str, login))}. "
         f"{'Cumple.' if login[:5] == [401] * 5 and login[5] == 429 else 'No cumple: se esperaba 401 en los intentos 1 a 5 y 429 en el 6.'}")
        if login else "No medido: ejecutar con `--base-url`.",
        "## Elementos que superan el umbral (Anexo C)",
        table(["Elemento medido", "Métrica", "Valor obtenido", "Umbral", "Estado", "Acción propuesta"],
              [(*row, action(row)) for row in found]) if found else "Ninguno.",
        "## Contraste de V(G) entre herramientas",
        "RNF-01 se evalúa con `lizard`. El medidor propio también cuenta `?.`, `??` y los patrones `and`/`or`, "
        f"y con ese conteo hay {len(roslyn_over)} métodos de C# por encima de 10:",
        table(["Clase", "Método", "V(G)"], [(r["clase"], r["metodo"], r["vg"]) for r in roslyn_over]) if roslyn_over else "Ninguno.",
    ]
    REPORT.write_text("\n\n".join(parts) + "\n", encoding="utf-8", newline="\n")


def main():
    parser = argparse.ArgumentParser(description="Mide el código de Budget Tracker.")
    parser.add_argument("--nombre", default="actual", help="Carpeta de docs/metricas donde se guarda la medición, por ejemplo antes o despues")
    parser.add_argument("--base-url", help="URL del portal en marcha, para probar el límite de inicio de sesión")
    parser.add_argument("--no-build", action="store_true", help="No compilar la API ni las migraciones antes de medir")
    args = parser.parse_args()

    global OUT, REPORT
    if not args.nombre.replace("-", "").isalnum():
        sys.exit("--nombre solo admite letras, números y guiones")
    OUT = ROOT / "docs" / "metricas" / args.nombre / "datos"
    REPORT = ROOT / "docs" / "metricas" / args.nombre / "resultados.md"
    OUT.mkdir(parents=True, exist_ok=True)
    if args.base_url:
        check_login(args.base_url.rstrip("/"))
    methods = measure_complexity()
    loc = measure_loc()
    duplication = measure_duplication()
    classes, roslyn = measure_design(not args.no_build)
    maintenance, unclassified = measure_maintenance()
    write_report(methods, loc, duplication, classes, roslyn, maintenance, unclassified, read_login())
    print(f"Listo: {REPORT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
