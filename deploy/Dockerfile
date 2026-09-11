FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto primero para aprovechar el cache de capas
COPY PortfolioOps.sln ./
COPY api/PortfolioOps.Api/PortfolioOps.Api.csproj api/PortfolioOps.Api/

RUN dotnet restore api/PortfolioOps.Api/PortfolioOps.Api.csproj

COPY . .

RUN dotnet publish api/PortfolioOps.Api/PortfolioOps.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

# No correr como root dentro del contenedor — la imagen base ya trae un
# usuario sin privilegios listo para usar via la env var $APP_UID.
USER $APP_UID

ENTRYPOINT ["dotnet", "PortfolioOps.Api.dll"]
