# 04 — Testing Standards

## Strategy

| Type | Tool | Scope | Annotation |
|------|------|-------|------------|
| Unit | JUnit 5 + Mockito | Single class, no context | none |
| Repository | JUnit 5 + H2 | JPA queries, schema | `@DataJpaTest` |
| Controller | MockMvc | HTTP + serialization | `@WebMvcTest` |
| E2E | RestAssured | Full app + real DB | `@SpringBootTest(webEnvironment=RANDOM_PORT)` |

## Naming
- Class: `<ClassUnderTest>Test`
- Method: `<method>_<condition>_<expectedResult>`
  - Example: `findById_whenNotFound_shouldThrowNotFoundException`

## Rules
- Mock only direct dependencies — use `@Mock` + `@InjectMocks`
- Never load Spring context for pure unit tests
- Coverage target: 80% minimum on `service/` package
- Arrange-Act-Assert structure, one assertion per test where possible
