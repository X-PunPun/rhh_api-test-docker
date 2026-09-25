# Imagen de la API (multi-etapa). Se construye desde la raíz del repositorio:
#   docker build -f src/RRHH.Api/Dockerfile .

# ---------- 1. Compilar ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /repo

# Primero solo los archivos de proyecto: si no cambian, Docker reutiliza la capa del restore.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/RRHH.Domain/RRHH.Domain.csproj src/RRHH.Domain/
COPY src/RRHH.Application/RRHH.Application.csproj src/RRHH.Application/
COPY src/RRHH.Infrastructure/RRHH.Infrastructure.csproj src/RRHH.Infrastructure/
COPY src/RRHH.Api/RRHH.Api.csproj src/RRHH.Api/
RUN dotnet restore src/RRHH.Api/RRHH.Api.csproj

COPY src/ src/
RUN dotnet publish src/RRHH.Api/RRHH.Api.csproj -c Release -o /publicado --no-restore

# ---------- 2. Ejecutar (imagen liviana, sin SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Datos de zonas horarias: la API calcula "hoy" con la hora de Chile (America/Santiago).
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=compilacion /publicado .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Usuario sin privilegios incluido en la imagen oficial.
USER $APP_UID
ENTRYPOINT ["dotnet", "RRHH.Api.dll"]
