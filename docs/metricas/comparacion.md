# Comparación antes y después

Compara [`antes`](./antes/resultados.md) con [`despues`](./despues/resultados.md). Generado por `metricas/comparar.py`; las definiciones están en [`fichas.md`](./fichas.md).

## Resumen

| Indicador | Antes | Después |
|---|---|---|
| Métodos medidos | 523 | 532 |
| Métodos con V(G) > 10 | 1 | 0 |
| V(G) máximo | 17 | 10 |
| Métodos con más de 50 líneas | 0 | 0 |
| Clases de la aplicación | 114 | 116 |
| Clases con WMC > 20 | 6 | 6 |
| Clases con CBO propio > 10 | 9 | 9 |
| Clases con LCOM* > 0.7 | 4 | 4 |
| Clases con MI < 65 | 15 | 16 |
| MI promedio | 79.17 | 78.68 |
| Duplicación en la API (%) | 2.63 | 2.56 |
| Mantenimiento preventivo (% de commits) | 0.0 | 0.8 |
| Inicio de sesión: 6 intentos fallidos | 401, 401, 401, 401, 401, 401 | 401, 401, 401, 401, 401, 429 |

## Elementos modificados

| Elemento | Métrica | Antes | Después |
|---|---|---|---|
| MovementRules.Validate | V(G) | 17 | 3 |
| MovementRules | WMC | 33 | 37 |
| MovementRules | CBO | 8 | 8 |
| MovementRules | MI | 43.89 | 40.41 |
| SignIn | WMC | 5 | 6 |
| SignIn | CBO | 12 | 14 |
| SignIn | MI | 93.29 | 84.62 |
| ModuleErrorFilter | WMC | 13 | 15 |
| ModuleErrorFilter | CBO | 20 | 25 |
| ModuleErrorFilter | MI | 65.66 | 61.59 |
