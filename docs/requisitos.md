# Requisitos

Budget Tracker permite a un pequeño negocio registrar sus ingresos y gastos, clasificarlos en
sus propias categorías, fijar presupuestos y conocer su situación financiera en una sola pantalla.

Cada requisito se deriva de una historia de usuario de
[`04-requirements/stories`](./04-requirements/stories), donde están sus escenarios completos. Las
rutas de la API llevan el prefijo `/api/v1`. Salvo en RF-01 y RF-02, toda operación exige una sesión
iniciada y solo alcanza los datos de la propia cuenta.

## Requisitos funcionales

| ID | Descripción | Criterios de aceptación | Origen |
|---|---|---|---|
| **RF-01** | El sistema debe permitir registrar una cuenta con nombre, correo y contraseña. | Una cuenta con datos válidos queda creada y puede iniciar sesión.<br>Un correo ya registrado se rechaza con `409` y no se crea otra cuenta.<br>Los campos inválidos se rechazan con `400`, indicando cada campo.<br>Enviar el rol `ADMIN` no concede ese rol: la cuenta queda como `USER`. | HU-01 |
| **RF-02** | El sistema debe permitir iniciar sesión con correo y contraseña. | Con credenciales correctas se entrega un token que la API acepta en las peticiones siguientes.<br>Con contraseña incorrecta o correo inexistente se responde `401`.<br>Una escritura sin token válido se responde `401` y no guarda nada. | HU-01 |
| **RF-03** | El sistema debe permitir crear categorías de ingreso o de gasto, y subcategorías dentro de ellas. | La categoría o subcategoría creada aparece en su lista como activa.<br>Un nombre repetido dentro del mismo tipo se rechaza con `409`; el mismo nombre en el otro tipo se acepta.<br>Un nombre de subcategoría repetido dentro de su categoría se rechaza con `409`. | HU-02 |
| **RF-04** | El sistema debe permitir gestionar el ciclo de vida de una categoría o subcategoría: renombrar la categoría y desactivar, reactivar o eliminar cualquiera de las dos. | **Renombrar:** los movimientos y presupuestos existentes muestran el nombre nuevo; un nombre ya usado en el tipo se rechaza con `409`.<br>**Eliminar:** una categoría con subcategorías o presupuestos, o una subcategoría con movimientos, no se elimina y se responde `409` (BR-05).<br>**Desactivar:** deja de ofrecerse para movimientos y presupuestos nuevos, y los movimientos existentes la siguen mostrando.<br>**Reactivar:** vuelve a ofrecerse. | HU-03 |
| **RF-05** | El sistema debe permitir registrar un movimiento con fecha, descripción y una o más líneas de detalle. | El movimiento válido queda guardado con el total de sus líneas.<br>El tipo de cada línea se toma de su subcategoría; el enviado por el cliente se ignora (BR-01).<br>Un movimiento que mezcla ingresos y gastos se rechaza con `422` y no guarda nada (BR-02).<br>Un monto de cero o menor se rechaza con `400`. | HU-04 |
| **RF-06** | El sistema debe permitir consultar el historial de movimientos. | Los movimientos aparecen del más reciente al más antiguo, con paginación.<br>Los movimientos eliminados no aparecen (BR-04). | HU-05 |
| **RF-07** | El sistema debe permitir eliminar un movimiento, conservándolo para auditoría. | El movimiento eliminado desaparece del historial y de los totales.<br>El registro se conserva con `deleted_by` y `deleted_at` (BR-04).<br>Eliminarlo por segunda vez responde `404`. | HU-06 |
| **RF-08** | El sistema debe permitir gestionar el presupuesto de una categoría por período: definirlo, consultarlo, cambiarlo y eliminarlo. | **Definir:** el presupuesto válido aparece en la lista de su categoría; un monto de cero o menor se rechaza con `400`, y un período que termina antes de empezar, con `422`.<br>**Cambiar:** un cambio válido se refleja en la lista; uno que rompe el período se rechaza y conserva los valores previos (BR-06).<br>**Eliminar:** deja de listarse y ya no impide eliminar su categoría (BR-05). | HU-07 |
| **RF-09** | El sistema debe presentar un panel con el total de ingresos, el total de gastos, el saldo neto y los movimientos más recientes. | El saldo es la diferencia entre ingresos y gastos, calculada en cada consulta (BR-03).<br>Se muestran los 5 movimientos más recientes.<br>Los movimientos eliminados no cuentan (BR-04).<br>Una cuenta sin movimientos muestra los tres indicadores en cero. | HU-08 |
| **RF-10** | El sistema debe permitir a un administrador listar las cuentas registradas. | Con rol `ADMIN` se listan las cuentas, sin contraseñas ni hashes.<br>Con rol `USER` se responde `403`, y sin token, `401`.<br>El rol `ADMIN` no da acceso a los datos de otra cuenta: se responde `404`. | HU-09 |

## Trazabilidad de los requisitos funcionales

Las pruebas nombradas son de `src/api/tests/end-to-end`; los casos `TC-NNN` están en
[`11-quality/test-cases`](./11-quality/test-cases). Las pantallas están en `src/portal/src/app/pages`.

| ID | Endpoint | Componente (API) | Pantalla | Pruebas |
|---|---|---|---|---|
| RF-01 | `POST /users` | `UsersController`, `SignUp`, `SignUpRules`, `UsersRepository` | `sign-up` | `PostUsers_newAccount_isCreatedWithItsLocation`<br>`PostUsers_emailInUse_isConflict`<br>`PostUsers_invalidFields_isValidationErrorWithEveryField`<br>`PostUsers_askingForAdmin_isStillAUser`<br>TC-001 |
| RF-02 | `POST /sessions` | `SessionsController`, `SignIn`, `PasswordHasher`, `TokenService` | `sign-in` | `PostSessions_rightPassword_returnsABearerTokenTheApiAccepts`<br>`PostSessions_wrongPasswordOrUnknownEmail_isUnauthorized`<br>`AnyOtherWrite_withoutAValidToken_isUnauthorized`<br>TC-013 |
| RF-03 | `POST /categories`<br>`POST /categories/{id}/subcategories` | `CategoriesController`, `ManageCategories`, `CategoryRules`, `CategoriesRepository` | `categories` | `CreateCategory_isCreatedWithItsLocation_andListed`<br>`CreateCategory_sameNameAndType_isConflict_otherTypeIsNot`<br>`CreateSubcategory_duplicateName_isConflict`<br>TC-002 |
| RF-04 | `PATCH` y `DELETE /categories/{id}`<br>`PATCH` y `DELETE /categories/{id}/subcategories/{id}` | `CategoriesController`, `ManageCategories`, `CategoryRules`, `CategoriesRepository` | `categories` | `DeleteCategory_withASubcategory_isConflict_thenDeactivate_thenDeleteBoth`<br>`RenameCategoryEndToEndTests`<br>TC-003, TC-021 |
| RF-05 | `POST /transactions` | `TransactionsController`, `ManageMovements`, `MovementRules`, `MovementsRepository` | `new-record` | `Record_expense_isCreated_withItsLocation_andTypedLines`<br>`Record_typeSentByTheClient_isIgnored`<br>`Record_mixingIncomeAndExpense_is422_namingTheLine`<br>`Record_badFields_is400_perField`<br>TC-004 |
| RF-06 | `GET /transactions` | `TransactionsController`, `ManageMovements`, `Paging`, `MovementsRepository` | `history` | `History_newestFirst_paged_andADeletedOneIsGone_thenDeletingAgainIs404`<br>TC-005 |
| RF-07 | `DELETE /transactions/{id}` | `TransactionsController`, `ManageMovements`, `MovementsRepository` | `history` | `History_newestFirst_paged_andADeletedOneIsGone_thenDeletingAgainIs404`<br>TC-006, TC-018 |
| RF-08 | `GET` y `POST /budgets`<br>`PATCH` y `DELETE /budgets/{id}` | `BudgetsController`, `ManageBudgets`, `BudgetRules`, `BudgetsRepository` | `budgets` | `Create_isCreatedWithItsLocation_andListedByCategory`<br>`Create_periodEndingBeforeItStarts_is422_andAZeroAmount_is400`<br>`TC019_overHttp_changeRefused_changeAccepted_categoryBlocked_deleted_categoryFree`<br>TC-007, TC-019 |
| RF-09 | `GET /dashboard` | `DashboardController`, `DashboardFacade`, `TransactionsFacade` | `dashboard` | `Dashboard_totalsAndRecentList_onRead_withinTwoSeconds`<br>`Dashboard_newAccount_isAllZeros_withNoRecentTransactions`<br>TC-008 |
| RF-10 | `GET /users` | `UsersController`, `ListUsers`, `UsersRepository` | `users` | `GetUsers_asAnAdmin_listsTheAccountsWithoutTheirHashes`<br>`GetUsers_asAUser_isForbidden`<br>`GetUsers_withoutAToken_isUnauthorized`<br>`AnAdmin_doesNotReachAnotherAccountsData`<br>TC-020 |

## Requisitos no funcionales

| ID | Categoría | Descripción medible | Forma de verificación |
|---|---|---|---|
| **RNF-01** | Mantenibilidad | Ningún método del código de la aplicación supera una complejidad ciclomática V(G) de 10. | Medición con `lizard` 1.25.0 sobre `src/api/src`, `src/db/src` y `src/portal/src/app`, sin archivos `.spec.ts`: cero métodos con V(G) mayor que 10. |
| **RNF-02** | Seguridad | Después de 5 intentos fallidos de inicio de sesión con un mismo correo en 15 minutos, los intentos siguientes de ese correo se rechazan con `429` hasta que termine la ventana. | Prueba automática end-to-end: los intentos 1 a 5 responden `401` y el intento 6 responde `429`, aunque la contraseña sea correcta. |
| **RNF-03** | Seguridad | El 100 % de las contraseñas se almacena como hash con sal; el texto plano nunca se guarda ni se escribe en los registros. | Pruebas automáticas `PostUsers_storesOnlyAHash_andNeverLogsThePassword` (TC-014) y `PasswordHasherTests`. Corresponde a NFR-06. |

### Estado frente a la entrega

La aplicación debe cumplir los tres antes de la entrega. RNF-01 y RNF-02 se corrigen después de la
medición inicial, para conservar la comparación antes y después.

| ID | Medición al 2026-11-08 | Estado | Acción |
|---|---|---|---|
| RNF-01 | 432 de 434 métodos cumplen. Superan el límite `MovementRules.Validate` (17) y `SignUpRules.Validate` (11). | No cumple | Extract Method en ambos métodos y nueva medición |
| RNF-02 | La API y nginx no tienen límite de intentos. | No cumple | Limitar los intentos en `POST /sessions` y agregar la prueba |
| RNF-03 | BCrypt con costo 12 y sal por hash; las pruebas pasan. | Cumple | Ninguna |

Las métricas de mantenibilidad y evolución, con sus fichas, se documentan aparte.
