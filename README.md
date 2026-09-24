# RRHH API

Módulo interno de **Recursos Humanos** para gestionar personal en Chile: empleados por región y comuna,
departamentos, cargos, jefaturas, vacaciones, seguros y reportes exportables a Excel.

Proyecto de portafolio construido con **ASP.NET Core 10**, **EF Core 10** y **SQL Server**, siguiendo
**arquitectura hexagonal** (puertos y adaptadores).

## Arquitectura

```
src/
 ├─ RRHH.Domain          Entidades y reglas de negocio (RUT, Empleado...). Sin dependencias.
 ├─ RRHH.Application     Casos de uso + puertos (interfaces). Solo depende de Domain.
 ├─ RRHH.Infrastructure  Adaptadores: EF Core / SQL Server, (luego) JWT, Excel.
 └─ RRHH.Api             Controllers, ProblemDetails, OpenAPI. Compone todo.
tests/
 ├─ RRHH.Domain.Tests
 ├─ RRHH.Application.Tests
 └─ RRHH.Api.IntegrationTests
```

Las dependencias apuntan hacia adentro: `Api → Infrastructure → Application → Domain`.

## Requisitos

- .NET 10 SDK
- Docker Desktop
- `dotnet tool install --global dotnet-ef`

## Puesta en marcha

```powershell
# 1. Base de datos (SQL Server en Docker, puerto 14333)
Copy-Item .env.example .env        # edita la contraseña
docker compose up -d

# 2. Cadena de conexión (queda fuera del repositorio)
dotnet user-secrets set "ConnectionStrings:RRHH" "Server=localhost,14333;Database=RRHH;User Id=sa;Password=<tu-clave>;TrustServerCertificate=True" --project src/RRHH.Api

# 3. Ejecutar (en Development aplica migraciones automáticamente)
dotnet run --project src/RRHH.Api
```

- Documentación interactiva: `https://localhost:7052/scalar`
- Estado: `https://localhost:7052/health`

## Problemas comunes

| Síntoma | Causa / solución |
|---|---|
| `Login failed for user 'sa'` | La clave en user-secrets no coincide con `MSSQL_SA_PASSWORD` del `.env`. Revisa con `dotnet user-secrets list --project src/RRHH.Api`. |
| Cambié la clave del `.env` y sigue fallando | SQL Server guarda la clave al crear el volumen. Recréalo: `docker compose down -v` y `docker compose up -d`. |
| El navegador queda cargando al presionar F5 | La API se detuvo al iniciar (ver ventana de salida). Corrige la conexión y vuelve a ejecutar. |

## Pruebas

```powershell
dotnet test
```

## Seguridad

Ver [SECURITY-ACCEPTANCE.md](SECURITY-ACCEPTANCE.md).

## Créditos de datos

Regiones y comunas (CUT) basadas en [jromerof/regiones-chile](https://github.com/jromerof/regiones-chile) (MIT),
con correcciones de códigos de Ñuble (16xxx), Huasco y ortografía.
