# bud-tack-api — the one deployable artifact (10-devops/ci-cd.md, `package`).
# Build:  docker build -t bud-tack-api:$(git rev-parse --short HEAD) .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props .editorconfig ./
COPY src/BudTack.Api/BudTack.Api.csproj src/BudTack.Api/
COPY src/BudTack.Identity/BudTack.Identity.csproj src/BudTack.Identity/
COPY src/BudTack.Transactions/BudTack.Transactions.csproj src/BudTack.Transactions/
COPY src/BudTack.Dashboard/BudTack.Dashboard.csproj src/BudTack.Dashboard/
RUN dotnet restore src/BudTack.Api/BudTack.Api.csproj
COPY src/ src/
RUN dotnet publish src/BudTack.Api/BudTack.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Listen on 3000, the port every contract uses; the portal's nginx proxies to
# it on the Compose network (ADR-006). Runs as the image's non-root user.
ENV ASPNETCORE_HTTP_PORTS=3000 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 3000
USER $APP_UID
ENTRYPOINT ["dotnet", "BudTack.Api.dll"]
