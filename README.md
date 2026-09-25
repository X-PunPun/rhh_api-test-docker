# RRHH API

Módulo interno de **Recursos Humanos** para gestionar personal en Chile: empleados por región y comuna,
departamentos, cargos, jefaturas, vacaciones (feriado legal y progresivo), seguros complementarios y
reportes exportables a Excel.

Proyecto de portafolio construido con **ASP.NET Core 10**, **EF Core 10** y **SQL Server**, siguiendo
**arquitectura hexagonal** (puertos y adaptadores).

## Levantar todo con Docker (recomendado para probar)

Con Docker Desktop abierto, **doble clic en `levantar.cmd`** (o `docker compose up -d --build`).
Levanta SQL Server, la API y la aplicación web ya conectadas entre sí:

| Servicio | URL |
|---|---|
| Aplicación web | http://localhost:8080 |
| Swagger (API) | http://localhost:5080/swagger |

La web (nginx) reenvía `/api` a la API, así el navegador habla con un solo origen. Para detener: `docker compose stop`.
Para depurar la API desde Visual Studio, levanta solo la base: `docker compose up -d sqlserver`.

## Aplicación web

El frontend está en [`frontend/`](frontend/README.md). Con la API corriendo:

```powershell
cd frontend
npm install
npm run dev     # http://localhost:5173
```

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
- *Puerto `IUsuarioActual`*: los casos de uso preguntan "¿quién eres?" sin saber de dónde sale; el adaptador lo lee
  del JWT. Al pasar de la identidad temporal a JWT no se modificó ningún caso de uso.

## Módulos y endpoints (v1)

| Módulo | Endpoints principales |
|---|---|
| Ubicación | `GET /regiones` · `GET /regiones/{id}/comunas` (16 regiones, 346 comunas con código CUT) |
| Organización | CRUD `/departamentos` · CRUD `/cargos` · activar/desactivar |
| Empleados | `GET /empleados` (filtros por región, comuna, depto., cargo, jefe, estado, texto/RUT + paginación) · ficha · `/subordinados` · crear · actualizar · desvincular |
| Autenticación | `POST /auth/login` · `/auth/renovar` · `/auth/logout` · `GET /auth/yo` · `POST /auth/cambiar-clave` |
| Usuarios (Admin) | CRUD `/usuarios` · rol y regiones · activar/desactivar · desbloquear · restablecer clave · `GET /auditoria` |
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

# 3. Llave para firmar los JWT (mínimo 32 caracteres, fuera del repositorio)
$llave = [Convert]::ToBase64String((1..48 | ForEach-Object { [byte](Get-Random -Maximum 256) }))
dotnet user-secrets set "Jwt:Llave" $llave --project src/RRHH.Api

# 4. Ejecutar (en Development aplica migraciones y carga datos y usuarios demo)
dotnet run --project src/RRHH.Api --launch-profile https
```

- Swagger: `https://localhost:7052/swagger`
- Scalar: `https://localhost:7052/scalar`
- Estado: `https://localhost:7052/health`

**Datos demo** (solo Development, si la base está vacía): 6 departamentos, 13 cargos, 17 empleados ficticios en
distintas regiones con jerarquía, planes de seguro y solicitudes de vacaciones. Ejemplos listos en `src/RRHH.Api/RRHH.Api.http`.

## Seguridad (autenticación y autorización)

Todo endpoint exige un **JWT** salvo `login`, `renovar`, `logout` y `/health`.

1. `POST /api/v1/auth/login` con email y clave → `tokenAcceso` (15 min) y `tokenRenovacion` (7 días).
2. En Swagger: botón **Authorize** → pegar el `tokenAcceso`.
3. `POST /api/v1/auth/renovar` entrega un par nuevo y revoca el anterior (si un token usado reaparece, se cierran todas las sesiones).

| Rol | Qué ve | Qué puede modificar |
|---|---|---|
| Admin | Todo | Todo + usuarios y auditoría |
| RRHH | Empleados de sus regiones asignadas | Personal, seguros y catálogos de sus regiones |
| Jefatura | Su ficha y su equipo directo (sin datos previsionales) | Aprueba o rechaza vacaciones de su equipo |
| Empleado | Solo su ficha | Solicita y cancela sus vacaciones |

Si un usuario pide un empleado fuera de su alcance recibe **404** (no se revela que existe). El mismo alcance se aplica en
búsquedas, fichas, subordinados, reportes y exportación Excel.

Otras medidas: bloqueo de 15 min tras 5 intentos fallidos, límite de solicitudes (login por IP y global por usuario),
claves PBKDF2, tokens de renovación guardados como hash, CORS con orígenes explícitos, cabeceras de seguridad,
`Cache-Control: no-store` y bitácora de auditoría (`GET /api/v1/auditoria`, solo Admin).

**Usuarios demo** (solo Development, clave `Demo.Rrhh2026`):

| Email | Rol |
|---|---|
| carolina.fuentes@empresa-demo.cl | Admin |
| marcela.soto@empresa-demo.cl | RRHH (todas las regiones) |
| diego.munoz@empresa-demo.cl | RRHH (Región Metropolitana) |
| valentina.reyes@empresa-demo.cl | RRHH (Valparaíso) |
| rodrigo.vergara@empresa-demo.cl | Jefatura (Tecnología) |
| matias.gonzalez@empresa-demo.cl | Empleado |

**Administrador inicial en otros ambientes:** si no existe ningún Admin, la API lo crea al iniciar con
`Seguridad:AdminInicial:Email` y `Seguridad:AdminInicial:Clave` (user-secrets o variables de entorno).

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
| `Falta 'Jwt:Llave'` al iniciar | Configura la llave con `dotnet user-secrets set "Jwt:Llave" ...` (paso 3). |
| 401 en todos los endpoints | Falta el token o expiró (15 min). Vuelve a hacer login o usa `/auth/renovar`. |

## Hoja de ruta

1. ✅ Backend: dominio, casos de uso, persistencia, reportes, pruebas.
2. ✅ Seguridad: JWT + token de renovación rotativo, roles, alcance por región/jefatura, bloqueo, rate limiting, CORS, auditoría.
3. ✅ Frontend: React + TypeScript + Vite en [`frontend/`](frontend/README.md).
4. ⏳ Calidad y despliegue: CI en GitHub Actions, auditoría final, Docker de la API y del front.

## Seguridad

Ver [SECURITY-ACCEPTANCE.md](SECURITY-ACCEPTANCE.md).

## Créditos de datos

Regiones y comunas (CUT) basadas en [jromerof/regiones-chile](https://github.com/jromerof/regiones-chile) (MIT),
con correcciones de códigos de Ñuble (16xxx), Huasco y ortografía.
