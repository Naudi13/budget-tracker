# Requisitos

Budget Tracker permite a un pequeño negocio registrar sus ingresos y gastos, clasificarlos en un
catálogo de categorías, fijar presupuestos y comparar lo gastado con lo planeado.

Una instalación sirve a un solo negocio ([ADR-009](./05-architecture/decisions/records/ADR-009-one-business-with-a-shared-catalog.md)).
Hay dos roles: `ADMIN` administra las cuentas, el catálogo y los presupuestos, y `USER` registra y
consulta sus propios movimientos. Nadie se registra por su cuenta: el administrador crea cada cuenta.

Cada requisito se deriva de una o más historias de usuario de
[`04-requirements/stories`](./04-requirements/stories), donde están sus escenarios completos. Las
rutas de la API llevan el prefijo `/api/v1`. Salvo el inicio de sesión y la confirmación de enlaces,
toda operación exige una sesión iniciada de una cuenta activa.

## Requisitos funcionales

| ID | Descripción | Criterios de aceptación | Origen |
|---|---|---|---|
| **RF-01** | El sistema debe permitir a un administrador gestionar las cuentas: crearlas, listarlas, inactivarlas, reactivarlas y eliminar las que nunca se activaron. | **Crear:** la cuenta queda pendiente y se entrega un enlace de activación; el administrador nunca ve ni define la contraseña. Un correo ya registrado se rechaza con `409`.<br>**Listar:** se muestran nombre, correo, rol y estado, sin contraseñas ni hashes.<br>**Inactivar:** la cuenta no puede iniciar sesión y su siguiente petición recibe `401`; el administrador no puede inactivar su propia cuenta (`409`).<br>**Eliminar:** solo una cuenta nunca activada; una con contraseña responde `409`.<br>Con rol `USER` se responde `403`. | HU-09, HU-13 |
| **RF-02** | El sistema debe permitir activar una cuenta o restablecer su contraseña mediante un enlace de un solo uso. | Con un enlace válido y una contraseña de 8 o más caracteres, la cuenta queda activa o la contraseña queda reemplazada.<br>Un enlace usado, reemplazado por otro o con más de 30 minutos se rechaza con el mismo mensaje. | HU-10 |
| **RF-03** | El sistema debe permitir iniciar sesión con correo y contraseña. | Con credenciales correctas de una cuenta activa se entrega un token que la API acepta.<br>Con contraseña incorrecta, correo inexistente o cuenta pendiente se responde `401`.<br>Una escritura sin token válido se responde `401` y no guarda nada. | HU-01 |
| **RF-04** | El sistema debe permitir a un administrador crear categorías de ingreso o de gasto, y subcategorías dentro de ellas. | La categoría o subcategoría creada aparece como activa y se ofrece a todos los usuarios.<br>Un nombre repetido dentro del mismo tipo se rechaza con `409`; el mismo nombre en el otro tipo se acepta.<br>Un nombre de subcategoría repetido dentro de su categoría se rechaza con `409`.<br>Con rol `USER` se responde `403`. | HU-02, HU-12 |
| **RF-05** | El sistema debe permitir a un administrador gestionar el ciclo de vida de una categoría o subcategoría: renombrar la categoría y desactivar, reactivar o eliminar cualquiera de las dos. | **Renombrar:** los movimientos y presupuestos existentes muestran el nombre nuevo; un nombre ya usado en el tipo se rechaza con `409`.<br>**Eliminar:** una categoría con subcategorías o presupuestos, o una subcategoría con movimientos, no se elimina y se responde `409` (BR-05).<br>**Desactivar:** deja de ofrecerse para movimientos y presupuestos nuevos, y los movimientos existentes la siguen mostrando.<br>**Reactivar:** vuelve a ofrecerse.<br>Con rol `USER` se responde `403`. | HU-03, HU-12 |
| **RF-06** | El sistema debe permitir registrar un movimiento con fecha, descripción y una o más líneas de detalle. | El movimiento válido queda guardado a nombre de quien lo registra, con el total de sus líneas.<br>El tipo de cada línea se toma de su subcategoría; el enviado por el cliente se ignora (BR-01).<br>Un movimiento que mezcla ingresos y gastos se rechaza con `422` y no guarda nada (BR-02).<br>Un monto de cero o menor se rechaza con `400`. | HU-04 |
| **RF-07** | El sistema debe permitir consultar el historial de movimientos según el rol. | Los movimientos aparecen del más reciente al más antiguo, con paginación.<br>Un `USER` ve solo los suyos; un `ADMIN` ve todos, con el nombre de quien los registró.<br>Los eliminados no aparecen en el historial (BR-04); solo un `ADMIN` puede consultarlos, con quién los eliminó, cuándo y por qué. | HU-05 |
| **RF-08** | El sistema debe permitir eliminar un movimiento indicando la razón, conservándolo para auditoría. | Sin una razón de al menos 3 caracteres no se elimina.<br>Un `USER` solo elimina los suyos; un `ADMIN`, cualquiera.<br>El movimiento eliminado desaparece del historial y de los totales, y se conserva con quién, cuándo y por qué (BR-04).<br>Eliminarlo por segunda vez responde `404`. | HU-06 |
| **RF-09** | El sistema debe permitir a un administrador gestionar el presupuesto de una categoría por período: definirlo, consultarlo, cambiarlo y eliminarlo. | **Definir:** el presupuesto válido aparece en la lista de su categoría; un monto de cero o menor se rechaza con `400`, y un período que termina antes de empezar, con `422`.<br>**Cambiar:** un cambio válido se refleja en la lista; uno que rompe el período se rechaza y conserva los valores previos (BR-06).<br>**Eliminar:** deja de listarse y ya no impide eliminar su categoría (BR-05).<br>Con rol `USER` se responde `403`. | HU-07, HU-12 |
| **RF-10** | El sistema debe presentar un panel con ingresos, gastos, saldo, movimientos recientes y avance frente al presupuesto, para todo el negocio o para una categoría. | El saldo es la diferencia entre ingresos y gastos, calculada en cada consulta (BR-03), y los movimientos eliminados no cuentan (BR-04).<br>Se muestran los 5 movimientos más recientes.<br>Se muestran el presupuesto vigente, lo gastado y lo disponible; si se excede, lo disponible es negativo, y sin presupuesto vigente se indica.<br>Al filtrar por una categoría, todo el panel se limita a ella.<br>Un `USER` ve sus propios totales y el presupuesto de todo el negocio. | HU-08, HU-11 |

## Trazabilidad de los requisitos funcionales

Las pruebas nombradas son de `src/api/tests/end-to-end`; los casos `TC-NNN` están en
[`11-quality/test-cases`](./11-quality/test-cases). Las pantallas están en `src/portal/src/app/pages`.

| ID | Endpoint | Componente (API) | Pantalla | Pruebas |
|---|---|---|---|---|
| RF-01 | `GET` y `POST /users`<br>`PATCH` y `DELETE /users/{id}`<br>`POST /users/{id}/activation-links` | `UsersController`, `CreateUser`, `ListUsers`, `ManageAccount`, `AccountRules`, `UsersRepository` | `users` | `PostUsers_asAnAdmin_createsAPendingAccount_withItsLink`<br>`PostUsers_emailInUse_isConflict`<br>`GetUsers_asAnAdmin_listsTheAccountsWithoutTheirHashes`<br>`Inactivating_endsTheSessionAtOnce_andActivatingRestoresTheAccount`<br>`AnAdministrator_cannotInactivateThemselves`<br>`Delete_onlyANeverActivatedAccount_andItsLinkStopsWorking`<br>TC-020, TC-022, TC-027 |
| RF-02 | `POST /account-links/confirm`<br>`POST /users/{id}/password-resets` | `AccountLinksController`, `AccountLinks`, `PasswordHasher` | `activate` | `AccountLink_worksOnce_andAPendingAccountCannotSignIn`<br>`AnExpiredLink_answersLikeAnyInvalidOne`<br>`PasswordReset_replacesThePassword_withoutTheAdminEverSeeingOne`<br>TC-023 |
| RF-03 | `POST /sessions` | `SessionsController`, `SignIn`, `PasswordHasher`, `TokenService` | `sign-in` | `PostSessions_rightPassword_returnsABearerTokenTheApiAccepts`<br>`PostSessions_wrongPasswordOrUnknownEmail_isUnauthorized`<br>`AnyOtherWrite_withoutAValidToken_isUnauthorized`<br>TC-013 |
| RF-04 | `POST /categories`<br>`POST /categories/{id}/subcategories` | `CategoriesController`, `ManageCategories`, `CategoryRules`, `CategoriesRepository` | `categories` | `CreateCategory_isCreatedWithItsLocation_andListed`<br>`CreateCategory_sameNameAndType_isConflict_otherTypeIsNot`<br>`CreateSubcategory_duplicateName_isConflict`<br>`AUser_isForbidden_andNothingChanges`<br>TC-002, TC-024 |
| RF-05 | `PATCH` y `DELETE /categories/{id}`<br>`PATCH` y `DELETE /categories/{id}/subcategories/{id}` | `CategoriesController`, `ManageCategories`, `CategoryRules`, `CategoriesRepository` | `categories` | `DeleteCategory_withASubcategory_isConflict_thenDeactivate_thenDeleteBoth`<br>`RenameCategoryEndToEndTests`<br>`AUser_isForbidden_andNothingChanges`<br>TC-003, TC-021, TC-024 |
| RF-06 | `POST /transactions` | `TransactionsController`, `ManageMovements`, `MovementRules`, `MovementsRepository` | `new-record` | `Record_expense_isCreated_withItsLocation_andTypedLines`<br>`Record_typeSentByTheClient_isIgnored`<br>`Record_mixingIncomeAndExpense_is422_namingTheLine`<br>`Record_badFields_is400_perField`<br>TC-004 |
| RF-07 | `GET /transactions` | `TransactionsController`, `ManageMovements`, `Paging`, `MovementsRepository` | `history` | `History_newestFirst_paged_andADeletedOneIsGone_thenDeletingAgainIs404`<br>`AUser_seesOnlyTheirs_andAnAdmin_seesAll_withWhoRecordedIt`<br>`AnotherUser_neverSeesOrDeletesMyTransactions_butSharesTheCatalog`<br>TC-005 |
| RF-08 | `POST /transactions/{id}/deletion` | `TransactionsController`, `ManageMovements`, `MovementsRepository` | `history` | `Deleting_needsAReason_onlyTheOwnerOrAnAdmin_andOnlyAnAdminReviewsIt`<br>`History_newestFirst_paged_andADeletedOneIsGone_thenDeletingAgainIs404`<br>TC-006, TC-018, TC-026 |
| RF-09 | `GET` y `POST /budgets`<br>`PATCH` y `DELETE /budgets/{id}` | `BudgetsController`, `ManageBudgets`, `BudgetRules`, `BudgetsRepository` | `budgets` | `Create_isCreatedWithItsLocation_andListedByCategory`<br>`Create_periodEndingBeforeItStarts_is422_andAZeroAmount_is400`<br>`TC019_overHttp_changeRefused_changeAccepted_categoryBlocked_deleted_categoryFree`<br>`AUser_isForbidden_andNothingChanges`<br>TC-007, TC-019, TC-024 |
| RF-10 | `GET /dashboard` | `DashboardController`, `DashboardFacade`, `TransactionsFacade` | `dashboard` | `Dashboard_totalsAndRecentList_onRead_withinTwoSeconds`<br>`Dashboard_newAccount_isAllZeros_withNoRecentTransactions`<br>`TheFilter_narrowsEverythingToOneCategory`<br>`OverBudget_isNegative_andNoBudgetInForce_isNull`<br>`AUsersTotalsAreTheirs_andTheBudgetIsTheBusinesss`<br>TC-008, TC-025 |

## Requisitos no funcionales

| ID | Categoría | Descripción medible | Forma de verificación |
|---|---|---|---|
| **RNF-01** | Mantenibilidad | Ningún método del código de la aplicación supera una complejidad ciclomática V(G) de 10. | Medición con `lizard` 1.25.0 sobre `src/api/src`, `src/db/src` y `src/portal/src/app`, sin archivos `.spec.ts`: cero métodos con V(G) mayor que 10. El job `complexity` del CI lo verifica en cada pull request. |
| **RNF-02** | Seguridad | Después de 5 intentos fallidos de inicio de sesión con un mismo correo en 15 minutos, los intentos siguientes de ese correo se rechazan con `429` hasta que termine la ventana. | Prueba automática end-to-end: los intentos 1 a 5 responden `401` y el intento 6 responde `429`, aunque la contraseña sea correcta. |
| **RNF-03** | Seguridad | El 100 % de las contraseñas se almacena como hash con sal; el texto plano nunca se guarda ni se escribe en los registros. | Pruebas automáticas `ActivationThenSignIn_storesOnlyAHash_andNeverLogsThePassword` (TC-014) y `PasswordHasherTests`. Corresponde a NFR-06. |

### Estado frente a la entrega

RNF-01 y RNF-02 no se cumplían en la medición inicial y se corrigieron después de ella, para conservar
la comparación ([`metricas/comparacion.md`](./metricas/comparacion.md)).

| ID | Antes (tag `metricas-antes`) | Después | Estado | Evidencia |
|---|---|---|---|---|
| RNF-01 | 522 de 523 métodos; `MovementRules.Validate` tenía 17 | 532 de 532 métodos; máximo 10 | Cumple | Extract Method en `MovementRules.Validate`; el job `complexity` del CI lo verifica en cada pull request |
| RNF-02 | Seis intentos fallidos respondían `401` | Los intentos 1 a 5 responden `401` y el 6 responde `429` | Cumple | `SignInAttempts` en `identity`; prueba `FiveFailures_thenEvenTheRightPassword_isTooManyRequests` y prueba en vivo con `medir.py --base-url` |
| RNF-03 | BCrypt con costo 12 y sal por hash | Sin cambios | Cumple | `ActivationThenSignIn_storesOnlyAHash_andNeverLogsThePassword` y `PasswordHasherTests` |

Las métricas de mantenibilidad y evolución, con sus fichas, se documentan aparte.
