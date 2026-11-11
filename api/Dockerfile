# =========================
# 1. BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files first so the restore layer is cached until dependencies change
COPY src/WeightTracker.Domain/WeightTracker.Domain.csproj src/WeightTracker.Domain/
COPY src/WeightTracker.Application/WeightTracker.Application.csproj src/WeightTracker.Application/
COPY src/WeightTracker.Infra/WeightTracker.Infra.csproj src/WeightTracker.Infra/
COPY src/WeightTracker.Migrations/WeightTracker.Migrations.csproj src/WeightTracker.Migrations/
COPY src/WeightTracker.Api/WeightTracker.Api.csproj src/WeightTracker.Api/
RUN dotnet restore src/WeightTracker.Api/WeightTracker.Api.csproj

COPY src/ src/
RUN dotnet publish src/WeightTracker.Api/WeightTracker.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# =========================
# 2. RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "WeightTracker.Api.dll"]
