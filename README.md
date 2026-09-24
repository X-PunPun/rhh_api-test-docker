# RRHH API

Módulo interno de **Recursos Humanos** para gestionar personal en Chile: empleados por región y comuna,
departamentos, cargos, jefaturas, vacaciones (feriado legal y progresivo), seguros complementarios y
reportes exportables a Excel.

Proyecto de portafolio construido con **ASP.NET Core 10**, **EF Core 10** y **SQL Server**, siguiendo
**arquitectura hexagonal** (puertos y adaptadores).

## Arquitectura

```
src/
 ├─ RRHH.Domain          Entidades y reglas de negocio puras (RUT, Empleado, Vacaciones, Seguros). Sin dependencias.
 ├─ RRHH.Application     Casos de uso + puertos (interfaces). Solo depende de Domain.
 ├─ RRHH.Infrastructure  Adaptadores de salida: EF Core / SQL Server, ClosedXML (Excel), datos semilla.
 └─ RRHH.Api             Adaptador de entrada: controladores REST, ProblemDetails, OpenAPI/Swagger.
tests/
 ├─ RRHH.Domain.Tests           Reglas de negocio (RUT, feriado legal, estados de solicitudes…)
 ├─ RRHH.Application.Tests      Casos de uso con dobles de prueba (sin base de datos)
 └─ RRHH.Api.IntegrationTests   API completa contra SQL Server real (Testcontainers)
```

Las dependencias apuntan hacia adentro: `Api → Infrastructure → Application → Domain`.

**Decisiones de diseño**

- *Repositorios para escritura, consultas para lectura* (CQRS liviano): las lecturas proyectan directo a DTO con EF (sin N+1).
- *Reglas en el dominio*: el controlador no valida negocio; la entidad no puede quedar en un estado inválido.
- *Errores estándar*: ProblemDetails (RFC 9457) → 400 regla de negocio · 403 sin permiso · 404 no existe · 409 conflicto.
- *Concurrencia optimista* (`rowversion`) en empleados y solicitudes.
- *Puerto `IUsuarioActual`*: hoy lo implementa un adaptador temporal (cabecera `X-Empleado-Id`); en la fase de
  seguridad se reemplaza por JWT **sin tocar los casos de uso**.

## Módulos y endpoints (v1)

| Módulo | Endpoints principales |
|---|---|
| Ubicación | `GET /regiones` · `GET /regiones/{id}/comunas` (16 regiones, 346 comunas con código CUT) |
| Organización | CRUD `/departamentos` · CRUD `/cargos` · activar/desactivar |
| Empleados | `GET /empleados` (filtros por región, comuna, depto., cargo, jefe, estado, texto/RUT + paginación) · ficha · `/subordinados` · crear · actualizar · desvincular |
| Vacaciones | saldo (`/empleados/{id}/vacaciones/saldo`) · solicitar · `/vacaciones/pendientes-equipo` · aprobar · rechazar · cancelar · `/feriados` |
| Seguros | CRUD `/planes-seguro` · afiliar / terminar (`/empleados/{id}/seguros`) |
| Reportes | `GET /reportes/resumen` (dashboard) · `GET /reportes/empleados/excel` |

Todas las rutas empiezan con `/api/v1`. Documentación interactiva en **Swagger** (`/swagger`) y **Scalar** (`/scalar`).

### Reglas de negocio destacadas (Chile)

- RUT validado con módulo 11 y único por empleado.
- Feriado legal: 15 días hábiles/año (1,25 por mes trabajado), sábados, domingos y feriados no se descuentan (Art. 67 y 69).
- Feriado progresivo: +1 día cada 3 años sobre 10, reconociendo hasta 10 años con empleadores anteriores (Art. 68).
- Solicitudes: sin superposición, con saldo suficiente, solo el propio empleado solicita/cancela y **solo la jefatura directa** aprueba o rechaza; nadie resuelve su propia solicitud.
- Jerarquía sin ciclos (A no puede ser jefe de B si B está sobre A).
- Exportación Excel con tope de filas y neutralización de inyección de fórmulas.

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

# 3. Ejecutar (en Development aplica migraciones y carga datos demo)
dotnet run --project src/RRHH.Api --launch-profile https
```

- Swagger: `https://localhost:7052/swagger`
- Scalar: `https://localhost:7052/scalar`
- Estado: `https://localhost:7052/health`

**Datos demo** (solo Development, si la base está vacía): 6 departamentos, 13 cargos, 17 empleados ficticios en
distintas regiones con jerarquía, planes de seguro y solicitudes de vacaciones. Ejemplos listos en `src/RRHH.Api/RRHH.Api.http`.

Para probar endpoints que requieren identidad, envía la cabecera `X-Empleado-Id` (Swagger la muestra como campo).
Ej.: el empleado 7 solicita vacaciones y el 5 (su jefe) las aprueba.

## Pruebas

```powershell
dotnet test                                   # todas (las de integración requieren Docker en ejecución)
dotnet test tests/RRHH.Domain.Tests           # solo dominio
```

## Problemas comunes

| Síntoma | Causa / solución |
|---|---|
| `Login failed for user 'sa'` | La clave en user-secrets no coincide con `MSSQL_SA_PASSWORD` del `.env`. Revisa con `dotnet user-secrets list --project src/RRHH.Api`. |
| Cambié la clave del `.env` y sigue fallando | SQL Server guarda la clave al crear el volumen. Recréalo: `docker compose down -v` y `docker compose up -d`. |
| `address already in use` al presionar F5 | Hay otra instancia corriendo (por ejemplo, `dotnet run` en una consola). Deténla con Ctrl+C. |
| Pruebas de integración fallan al iniciar | Docker Desktop no está en ejecución. |

## Hoja de ruta

1. ✅ Backend: dominio, casos de uso, persistencia, reportes, pruebas.
2. ⏳ Seguridad: Identity + JWT + refresh token, roles (Admin, RRHH, Jefatura, Empleado), alcance por región/jefatura, rate limiting, CORS, auditoría.
3. ⏳ Frontend.

## Seguridad

Ver [SECURITY-ACCEPTANCE.md](SECURITY-ACCEPTANCE.md).

## Créditos de datos

Regiones y comunas (CUT) basadas en [jromerof/regiones-chile](https://github.com/jromerof/regiones-chile) (MIT),
con correcciones de códigos de Ñuble (16xxx), Huasco y ortografía.
