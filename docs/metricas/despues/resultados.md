# Resultados de las métricas

Generado por `metricas/medir.py` sobre el commit `16d5308`. Las definiciones están en [`fichas.md`](../fichas.md) y los datos completos en [`datos/`](./datos).

## Complejidad ciclomática V(G)

| Parte | Métodos | 1–10 | 11–20 | 21–50 | > 50 | Promedio | Máximo |
|---|---|---|---|---|---|---|---|
| api | 224 | 224 | 0 | 0 | 0 | 2.04 | 10 |
| db | 36 | 36 | 0 | 0 | 0 | 1.5 | 10 |
| portal | 272 | 272 | 0 | 0 | 0 | 1.75 | 8 |

## Líneas de código

| Parte | Archivos | Físicas | Efectivas | Comentarios | En blanco | % comentarios |
|---|---|---|---|---|---|---|
| api | 115 | 4112 | 3018 | 524 | 570 | 14.8 |
| db | 18 | 777 | 542 | 124 | 111 | 18.6 |
| portal | 29 | 2521 | 2020 | 187 | 314 | 8.5 |

Métodos con más de 50 líneas efectivas: 0.

## Diseño: suite CK e Índice de Mantenibilidad

133 clases de C# medidas. MI promedio: 80.39 (mínimo 32.59), sobre 76 clases con métodos. LCOM* calculable en 18 clases.

### Clases con mayor WMC

| Clase | WMC | DIT | NOC | CBO | CBO propio | RFC | LCOM* | MI |
|---|---|---|---|---|---|---|---|---|
| MovementRules | 37 | 0 | 0 | 8 | 8 | 21 | n/a | 40.41 |
| CategoriesRepository | 26 | 0 | 0 | 14 | 7 | 29 | 0.00 | 32.59 |
| BudgetRules | 25 | 0 | 0 | 8 | 8 | 11 | n/a | 50.31 |
| AccountRules | 24 | 0 | 0 | 7 | 7 | 10 | n/a | 59.67 |
| CategoryRules | 24 | 0 | 0 | 7 | 7 | 13 | n/a | 61.82 |
| JsonLogFormatter | 23 | 1 | 0 | 5 | 1 | 25 | n/a | 53.55 |
| UsersRepository | 18 | 0 | 0 | 12 | 3 | 33 | 0.00 | 42.86 |
| TransactionsFacade | 18 | 0 | 0 | 20 | 20 | 36 | 0.80 | 52.61 |
| ManageMovements | 17 | 0 | 0 | 22 | 18 | 38 | 0.78 | 53.93 |
| MovementsRepository | 17 | 0 | 0 | 20 | 8 | 44 | 0.00 | 35.71 |

### Clases con mayor CBO

| Clase | WMC | DIT | NOC | CBO | CBO propio | RFC | LCOM* | MI |
|---|---|---|---|---|---|---|---|---|
| ModuleErrorFilter | 15 | 0 | 0 | 25 | 17 | 10 | n/a | 61.59 |
| ManageMovements | 17 | 0 | 0 | 22 | 18 | 38 | 0.78 | 53.93 |
| ErrorResponses | 12 | 0 | 0 | 20 | 3 | 21 | n/a | 64.48 |
| Identity004_AddAccountTokens | 2 | 2 | 0 | 20 | 0 | 25 | n/a | 76.83 |
| Transactions005_AddDetailLineNumber | 2 | 2 | 0 | 20 | 0 | 21 | n/a | 81.06 |
| MovementsRepository | 17 | 0 | 0 | 20 | 8 | 44 | 0.00 | 35.71 |
| TransactionsFacade | 18 | 0 | 0 | 20 | 20 | 36 | 0.80 | 52.61 |
| CategoriesController | 8 | 1 | 0 | 19 | 9 | 19 | 0.00 | 65.98 |
| UsersController | 6 | 1 | 0 | 19 | 8 | 16 | 0.00 | 79.69 |
| IdentityModule | 1 | 0 | 0 | 19 | 14 | 17 | n/a | 70.60 |

### Clases con menor MI

| Clase | WMC | DIT | NOC | CBO | CBO propio | RFC | LCOM* | MI |
|---|---|---|---|---|---|---|---|---|
| CategoriesRepository | 26 | 0 | 0 | 14 | 7 | 29 | 0.00 | 32.59 |
| MovementsRepository | 17 | 0 | 0 | 20 | 8 | 44 | 0.00 | 35.71 |
| MovementRules | 37 | 0 | 0 | 8 | 8 | 21 | n/a | 40.41 |
| UsersRepository | 18 | 0 | 0 | 12 | 3 | 33 | 0.00 | 42.86 |
| BudgetsRepository | 14 | 0 | 0 | 12 | 5 | 27 | 0.00 | 46.03 |
| BudgetRules | 25 | 0 | 0 | 8 | 8 | 11 | n/a | 50.31 |
| ManageCategories | 16 | 0 | 0 | 18 | 14 | 37 | 0.19 | 51.11 |
| TransactionsFacade | 18 | 0 | 0 | 20 | 20 | 36 | 0.80 | 52.61 |
| JsonLogFormatter | 23 | 1 | 0 | 5 | 1 | 25 | n/a | 53.55 |
| ManageMovements | 17 | 0 | 0 | 22 | 18 | 38 | 0.78 | 53.93 |

## Costo de mantenimiento

| Tipo | Commits | Líneas cambiadas | % de commits | % clásico |
|---|---|---|---|---|
| Correctivo | 14 | 617 | 11.9 | 21 |
| Adaptativo | 21 | 3867 | 17.8 | 25 |
| Perfectivo | 82 | 17477 | 69.5 | 50 |
| Preventivo | 1 | 74 | 0.8 | 4 |

Fuera de la clasificación: 148 commits (documentación, pruebas y mensajes sin tipo).

## Duplicación (sin migraciones)

| Parte | % duplicado |
|---|---|
| api | 2.56 |
| db | 0.0 |
| portal | 0.0 |

## Límite de intentos de inicio de sesión (RNF-02)

Respuestas a 6 intentos fallidos seguidos con un mismo correo: 401, 401, 401, 401, 401, 429. Cumple.

## Elementos que superan el umbral (Anexo C)

| Elemento medido | Métrica | Valor obtenido | Umbral | Estado | Acción propuesta |
|---|---|---|---|---|---|
| ApiDocumentation | CBO | 15 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| BearerAuthentication | CBO | 13 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| BearerAuthentication.Handler | CBO | 11 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| BudgetsController | CBO | 16 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| CategoriesController | CBO | 19 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| ErrorResponses | CBO | 20 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| ErrorResponses | MI | 64.48 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| ErrorResponses.UnhandledExceptionHandler | CBO | 13 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| HealthEndpoint | CBO | 11 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| JsonLogFormatter | WMC | 23 | ≤ 20 | Supera | Extract Class o Extract Method |
| JsonLogFormatter | MI | 53.55 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| ModuleErrorFilter | CBO | 25 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| ModuleErrorFilter | MI | 61.59 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| SessionsController | CBO | 11 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| TransactionsController | CBO | 13 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| UsersController | CBO | 19 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| Identity001_CreateUsers | CBO | 17 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Identity002_AddUserRole | CBO | 12 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Identity004_AddAccountTokens | CBO | 20 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Identity005_AddUserIsActive | CBO | 11 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Transactions001_CreateCategories | CBO | 12 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Transactions002_CreateSubcategories | CBO | 12 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Transactions003_CreateTransactions | CBO | 12 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Transactions004_CreateTransactionDetails | CBO | 16 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Transactions005_AddDetailLineNumber | CBO | 20 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| Transactions006_CreateBudgets | CBO | 16 | ≤ 10 | Supera | Ninguna: una migración aplicada no se modifica |
| AccountLinks | CBO | 17 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| AccountLinks | LCOM* | 0.88 | ≤ 0.7 | Supera | Extract Class |
| AccountRules | WMC | 24 | ≤ 20 | Supera | Extract Class o Extract Method |
| AccountRules | MI | 59.67 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| CreateUser | CBO | 13 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| IdentityFacade | CBO | 17 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| IdentityFacade | LCOM* | 0.94 | ≤ 0.7 | Supera | Extract Class |
| IdentityFacade | MI | 64.10 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| IdentityModule | CBO | 19 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| ManageAccount | CBO | 12 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| SignIn | CBO | 14 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| UsersRepository | CBO | 12 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| UsersRepository | MI | 42.86 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| BudgetRules | WMC | 25 | ≤ 20 | Supera | Extract Class o Extract Method |
| BudgetRules | MI | 50.31 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| BudgetsRepository | CBO | 12 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| BudgetsRepository | MI | 46.03 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| CategoriesRepository | WMC | 26 | ≤ 20 | Supera | Extract Class o Extract Method |
| CategoriesRepository | CBO | 14 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| CategoriesRepository | MI | 32.59 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| CategoryRules | WMC | 24 | ≤ 20 | Supera | Extract Class o Extract Method |
| CategoryRules | MI | 61.82 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| ManageBudgets | CBO | 17 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| ManageBudgets | MI | 61.75 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| ManageCategories | CBO | 18 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| ManageCategories | MI | 51.11 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| ManageMovements | CBO | 22 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| ManageMovements | LCOM* | 0.78 | ≤ 0.7 | Supera | Extract Class |
| ManageMovements | MI | 53.93 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| MovementRules | WMC | 37 | ≤ 20 | Supera | Extract Class o Extract Method |
| MovementRules | MI | 40.41 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| MovementsRepository | CBO | 20 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| MovementsRepository | MI | 35.71 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| TransactionsFacade | CBO | 20 | ≤ 10 | Supera | Inyección de dependencias o Facade |
| TransactionsFacade | LCOM* | 0.80 | ≤ 0.7 | Supera | Extract Class |
| TransactionsFacade | MI | 52.61 | ≥ 65 | Baja | Extract Method y reducir tamaño |
| TransactionsModule | CBO | 18 | ≤ 10 | Supera | Inyección de dependencias o Facade |

## Contraste de V(G) entre herramientas

RNF-01 se evalúa con `lizard`. El medidor propio también cuenta `?.`, `??` y los patrones `and`/`or`, y con ese conteo hay 1 métodos de C# por encima de 10:

| Clase | Método | V(G) |
|---|---|---|
| ModuleErrorFilter | OnException | 13 |
