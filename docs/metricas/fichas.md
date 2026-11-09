# Fichas de métricas

Todas las mediciones se obtienen con un solo comando, descrito en
[`metricas/README.md`](../../metricas/README.md). Cada medición se guarda en su carpeta: la inicial en [`antes/`](./antes/resultados.md).
Los umbrales son los de la tabla de la guía del curso, salvo donde se indica.

## 1. Complejidad ciclomática V(G)

| Campo | Contenido |
|---|---|
| Atributo que mide | Complejidad de cada método |
| Fórmula | Puntos de decisión + 1 (McCabe) |
| Umbral e interpretación | 1–10 riesgo bajo; 11–20 moderado; 21–50 alto; más de 50 muy alto. El proyecto exige 10 o menos (RNF-01) |
| Herramienta | `lizard` 1.25.0: `lizard -l csharp src/api/src --csv`, y lo mismo para `src/db/src` y `src/portal/src/app` |
| Alcance | Todos los métodos de la API, las migraciones y el portal, sin pruebas |
| Justificación | Es la métrica estructural de la guía y verifica directamente RNF-01 |
| Acción si se supera | Extract Method |

## 2. Líneas de código y porcentaje de comentarios

| Campo | Contenido |
|---|---|
| Atributo que mide | Tamaño y proporción de documentación |
| Fórmula | Físicas = efectivas + comentarios + en blanco. % comentarios = comentarios / (efectivas + comentarios) × 100. Una línea con código y comentario cuenta como efectiva |
| Umbral e interpretación | Más de 50 líneas efectivas por método es una señal de alerta. La guía no fija un umbral de comentarios |
| Herramienta | Conteo por archivo en `metricas/medir.py`; líneas por método con `lizard` 1.25.0 |
| Alcance | Igual que V(G) |
| Justificación | Los métodos largos concentran responsabilidades y el porcentaje muestra qué tan documentado está el código |
| Acción si se supera | Extract Method |

## 3. Índice de Mantenibilidad (MI)

| Campo | Contenido |
|---|---|
| Atributo que mide | Mantenibilidad global de cada clase |
| Fórmula | MI = 171 − 5.2 × ln(V) − 0.23 × G − 16.2 × ln(LOC), limitado al rango 0–100. V es el Volumen de Halstead, G el WMC y LOC las líneas con código de los métodos |
| Umbral e interpretación | Cercano a 100: alta. Menor de 65: baja |
| Herramienta | Medidor propio `metricas/BudTack.Metrics`, con `Microsoft.CodeAnalysis.CSharp` 5.0.0. `Microsoft.CodeAnalysis.Metrics` 4.14 y 5.0 no analizaron los proyectos .NET 10 |
| Alcance | Clases, registros y estructuras de C# con al menos un método. Se excluye el portal |
| Justificación | Resume en un indicador el tamaño, la complejidad y el vocabulario del código |
| Acción si se supera | Extract Method y reducir el tamaño de la clase |

El índice se calcula por clase, así que penaliza a las clases grandes aunque sus métodos sean simples.

## 4. Suite CK

| Campo | Contenido |
|---|---|
| Atributo que mide | Calidad del diseño orientado a objetos |
| Fórmula | **WMC:** suma de V(G) de los métodos. **DIT:** clases base hasta `object`, incluidas las del framework. **NOC:** subclases directas en el proyecto. **CBO:** tipos distintos que usa la clase, sin ella misma, tipos primitivos ni `System.*`. **CBO propio:** la parte de CBO que son tipos del proyecto. **RFC:** métodos propios más métodos distintos que invoca. **LCOM\*:** Henderson-Sellers, (promedio de μ(A) − m) / (1 − m), donde m son los métodos de instancia y μ(A) cuántos usan cada atributo |
| Umbral e interpretación | WMC > 20; DIT > 5; CBO > 10; RFC > 50; LCOM > 0.7 |
| Herramienta | Medidor propio `metricas/BudTack.Metrics`, con análisis semántico de Roslyn |
| Alcance | Las mismas clases que MI. LCOM\* solo se calcula con dos o más métodos de instancia y al menos un atributo |
| Justificación | La aplicación está escrita en C#, un lenguaje orientado a objetos, y estas métricas evalúan el acoplamiento y la cohesión que la arquitectura busca |
| Acción si se supera | Extract Class para baja cohesión o WMC alto; inyección de dependencias o Facade para alto acoplamiento |

El medidor cuenta en V(G) también `?.`, `??` y los patrones `and` y `or`, por lo que su WMC puede ser
mayor que la suma de los valores de `lizard`. Una fachada delega cada método en un caso de uso
distinto, así que su LCOM\* alto es esperable por diseño.

## 5. Costo de mantenimiento por tipo

| Campo | Contenido |
|---|---|
| Atributo que mide | Distribución del esfuerzo de evolución |
| Fórmula | Commits sin merges y líneas cambiadas por tipo. `fix` es correctivo; `chore`, `build` y `ci`, adaptativo; `feat`, perfectivo; `refactor`, preventivo. Se excluyen los archivos `package-lock.json` y `.png` |
| Umbral e interpretación | Se compara con la distribución clásica: 21 % correctivo, 25 % adaptativo, 50 % perfectivo y 4 % preventivo |
| Herramienta | `git log --no-merges --numstat`, en `metricas/medir.py` |
| Alcance | Todo el historial de la rama medida. `docs`, `test` y los mensajes sin tipo quedan fuera de la clasificación |
| Justificación | Es la métrica de evolución y se obtiene del registro real del proyecto |
| Acción si se supera | Registrar el mantenimiento preventivo pendiente en issues etiquetados |

Los commits son una aproximación del esfuerzo, porque no se registraron horas. El proyecto está en
construcción, así que `feat` incluye desarrollo inicial además de mejoras.

## 6. Duplicación

| Campo | Contenido |
|---|---|
| Atributo que mide | Código repetido |
| Fórmula | Porcentaje de código en bloques duplicados |
| Umbral e interpretación | La guía no fija un umbral; se usa como métrica complementaria |
| Herramienta | `lizard` 1.25.0 con la extensión `-Eduplicate` |
| Alcance | Igual que V(G), sin migraciones: una migración aplicada no se modifica |
| Justificación | Agregada por el proyecto: el código repetido obliga a corregir lo mismo en varios lugares |
| Acción si se supera | Extract Method o un proyecto compartido |
