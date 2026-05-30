# Build context is the repository root so the shared Directory.*.props are available.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first against just the project file + central package management
# so the dependency layer is cached across source changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY backend/src/InventoryManager.Api/InventoryManager.Api.csproj backend/src/InventoryManager.Api/
RUN dotnet restore backend/src/InventoryManager.Api/InventoryManager.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/src/InventoryManager.Api/InventoryManager.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "InventoryManager.Api.dll"]
