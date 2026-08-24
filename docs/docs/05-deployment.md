# 05 — Deployment

## Build

```bash
mvn clean package -DskipTests
# Output: target/<app>-<version>.jar
```

## Configuration
- All environment-specific values via environment variables — never hardcoded
- Spring profiles: `dev`, `staging`, `prod`
- `application.yml` holds defaults; `application-prod.yml` holds prod overrides

## Dockerfile

```dockerfile
FROM eclipse-temurin:21-jre-alpine
WORKDIR /app
COPY target/*.jar app.jar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
```

## Health Check
Expose `/actuator/health`. Required before deployment is declared stable.

## Pipeline Steps
1. Compile + unit tests
2. Static analysis (SonarQube)
3. Build Docker image + push to registry
4. Deploy to staging — smoke tests
5. Deploy to production (manual gate)
