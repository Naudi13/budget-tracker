# Análisis de resultados y plan de mejoras

Interpreta la medición [`despues`](./despues/resultados.md) frente a los umbrales de las
[fichas](./fichas.md), identifica los problemas de mantenibilidad y propone mejoras priorizadas. La
evolución desde la medición inicial está en [`comparacion.md`](./comparacion.md). Salvo que se indique,
las cifras de diseño son de las 116 clases de la aplicación, sin migraciones.

## 1. Interpretación frente a los umbrales

| Métrica | Umbral | Resultado | Lectura |
|---|---|---|---|
| V(G) por método | ≤ 10 (RNF-01) | 532 métodos; máximo 10; promedio 2.04 en la API, 1.5 en migraciones y 1.75 en el portal | Cumple. La lógica está repartida en métodos pequeños |
| Líneas por método | ≤ 50 | Ningún método las supera | Cumple |
| % de comentarios | Sin umbral | 14.8 % en la API, 18.6 % en migraciones y 8.5 % en el portal | El portal está poco documentado frente al resto |
| WMC | ≤ 20 | 6 clases lo superan | Problema 1 |
| MI | ≥ 65 | 16 clases por debajo; mínimo 32.59 | Problemas 1, 2 y 3 |
| CBO | ≤ 10 | 27 clases lo superan; en 18 de ellas el CBO propio es 10 o menos | Problema 3 en las 9 restantes; problema 4 en el resto |
| LCOM\* | ≤ 0.7 | 4 clases lo superan | Problema 3 |
| RFC | ≤ 50 | Máximo 44 | Cumple |
| DIT y NOC | DIT ≤ 5 | DIT máximo 1; NOC 0 | Cumple. El diseño usa composición, no herencia |
| Duplicación | Sin umbral | 2.56 % en la API; 0 % en migraciones y portal | Problema 5 |
| Costo de mantenimiento | 21 / 25 / 50 / 4 % | Correctivo 11.9 %, adaptativo 17.8 %, perfectivo 69.5 %, preventivo 0.8 % | Problema 6 |

La complejidad por método, la profundidad de herencia y la respuesta de las clases están dentro de los
umbrales. Los problemas están en el tamaño de algunas clases, en el acoplamiento de los casos de uso
y en la falta de mantenimiento preventivo.

## 2. Problemas detectados

### Problema 1: clases de reglas con muchas responsabilidades

| Clase | WMC | Métodos | MI |
|---|---|---|---|
| `MovementRules` | 37 | 7 | 40.41 |
| `BudgetRules` | 25 | 5 | 50.31 |
| `CategoryRules` | 24 | 7 | 61.82 |
| `AccountRules` | 24 | 3 | 59.67 |

Ninguno de sus métodos supera V(G) 10: el WMC alto viene de reunir en una clase la validación de la
forma de los datos y las reglas de negocio. `MovementRules`, por ejemplo, valida la petición, la razón
de eliminación y clasifica las líneas según BR-01, BR-02 y BR-05.

### Problema 2: repositorios con Índice de Mantenibilidad bajo

| Clase | MI | Líneas con código | WMC |
|---|---|---|---|
| `CategoriesRepository` | 32.59 | 183 | 26 |
| `MovementsRepository` | 35.71 | 172 | 17 |
| `UsersRepository` | 42.86 | 120 | 18 |
| `BudgetsRepository` | 46.03 | 112 | 14 |

Su complejidad es moderada; el MI baja por el tamaño y por el volumen de Halstead de las consultas
SQL escritas dentro de los métodos. `CategoriesRepository` además maneja categorías y subcategorías y
concentra los bloques duplicados de la API.

### Problema 3: acoplamiento y cohesión en casos de uso y fachadas

| Clase | CBO | CBO propio | LCOM\* | MI |
|---|---|---|---|---|
| `TransactionsFacade` | 20 | 20 | 0.80 | 52.61 |
| `ManageMovements` | 22 | 18 | 0.78 | 53.93 |
| `IdentityFacade` | 17 | 17 | 0.94 | 64.10 |
| `ModuleErrorFilter` | 25 | 17 | n/a | 61.59 |
| `AccountLinks` | 17 | 13 | 0.88 | 65.74 |

En las fachadas, el LCOM\* alto es consecuencia del patrón: cada método delega en un caso de uso
distinto y no comparten atributos. En `ManageMovements` sí indica dos responsabilidades: registrar
movimientos y consultarlos o eliminarlos según el rol. `ModuleErrorFilter` traduce cada excepción de
los módulos con un caso de `switch`, así que crece con cada error nuevo.

### Problema 4: acoplamiento con el framework

En 18 de las 27 clases con CBO mayor que 10, el acoplamiento con tipos del proyecto es de 10 o menos: lo
que las lleva sobre el umbral son tipos de ASP.NET Core, Npgsql o la configuración, como en los
controladores. A esas se suman 10 de las 17 migraciones. No es un problema
de diseño: es el costo de usar el framework, y no se propone acción.

### Problema 5: duplicación

El 2.56 % de duplicación de la API está en `CategoriesRepository`, que repite bloques de lectura de
filas. Además, `PostgresUrl` y `DotEnv` existen casi iguales en la API y en las migraciones.

### Problema 6: mantenimiento preventivo casi inexistente

Solo hay un commit preventivo (0.8 %, frente al 4 % clásico) y el perfectivo llega a 69.5 %. El proyecto
sigue en construcción, así que gran parte del trabajo es desarrollo nuevo; pero la deuda de los
problemas 1 a 5 no tenía registro ni tiempo asignado.

## 3. Mejoras implementadas

| Mejora | Técnica | Antes | Después |
|---|---|---|---|
| `MovementRules.Validate` | Extract Method | V(G) 17 | V(G) 3; máximo del proyecto 10 |
| Límite de intentos de inicio de sesión | Nueva clase `SignInAttempts` | Seis `401` | Cinco `401` y un `429` |
| Verificación continua de RNF-01 | Job `complexity` en el CI | Solo en la medición | En cada pull request |

Las pruebas no cambiaron y todas pasan, así que el comportamiento se conserva. Como efecto
secundario, el WMC de `MovementRules` subió de 33 a 37 y su MI bajó de 43.89 a 40.41: Extract Method
reparte la complejidad en más métodos de la misma clase. El nuevo caso de error bajó el MI de
`ModuleErrorFilter` de 65.66 a 61.59. Estas dos clases encabezan el plan siguiente.

## 4. Plan de mejoras

Ordenado por prioridad: primero las de riesgo y esfuerzo bajos. Los efectos son estimaciones y se
confirman con una nueva medición.

| # | Problema | Mejora | Técnica | Riesgo | Esfuerzo | Efecto esperado |
|---|---|---|---|---|---|---|
| 1 | 1 | Separar en `MovementRules` la validación de la petición de la clasificación de líneas | Extract Class | Bajo: código puro con pruebas unitarias | Bajo | WMC 37 → dos clases de 20 o menos; MI sobre 50 |
| 2 | 3 | Reemplazar el `switch` de `ModuleErrorFilter` por una tabla de correspondencias entre excepción y respuesta | Replace Conditional with Polymorphism | Bajo: lo cubren las pruebas end-to-end | Bajo | WMC 15 → menos de 5; MI ≥ 65 |
| 3 | 1 | Extraer en `BudgetRules`, `CategoryRules` y `AccountRules` la validación de cada campo | Extract Method | Bajo | Medio | WMC bajo 20 en las tres |
| 4 | 3 | Dividir `ManageMovements` en registro y consulta o eliminación | Extract Class | Medio: toca la composición del módulo | Medio | CBO propio 18 → 10 o menos por clase; LCOM\* bajo 0.7 |
| 5 | 2 y 5 | Separar las subcategorías de `CategoriesRepository` y extraer la lectura de filas repetida | Extract Class y Extract Method | Medio: acceso a datos | Medio | MI de 32.59 a más de 50; duplicación de la API cerca de 0 % |
| 6 | 2 | Sacar el SQL de los demás repositorios a constantes con nombre | Extract Constant | Bajo | Medio | MI de los repositorios sobre 50 |
| 7 | 5 | Mover `PostgresUrl` y `DotEnv` a un proyecto compartido por la API y las migraciones | Extract Module | Medio: dos soluciones | Medio | Una sola implementación que mantener |
| 8 | 6 | Registrar la deuda como issues con etiquetas `correctivo`, `adaptativo`, `perfectivo` y `preventivo`, y reservar tiempo para las preventivas | Proceso | Ninguno | Bajo | Preventivo cerca del 4 % y costo medido por issue |

No se propone cambiar:

- **El LCOM\* de las fachadas:** su baja cohesión es la forma esperada de una fachada que delega.
- **El CBO causado por el framework (problema 4):** reducirlo exigiría envolver el framework sin beneficio.
- **Las migraciones:** una migración aplicada no se modifica.
