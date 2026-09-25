# RRHH Web

Frontend del módulo de Recursos Humanos: **React 19 + TypeScript + Vite**, consumiendo la API `RRHH.Api`.

## Stack

| Necesidad | Librería | Por qué |
|---|---|---|
| Rutas | React Router | Rutas anidadas con layout y rutas protegidas por rol |
| Datos del servidor | TanStack Query | Caché, reintentos, invalidación tras guardar, estados de carga |
| Formularios | React Hook Form + Zod | Validación inmediata en el navegador (RUT, fechas, 18 años) |
| Pruebas | Vitest + Testing Library | Mismo motor que Vite, rápido |

Estilos: CSS propio con variables (tema claro y oscuro automático), sin framework de UI.
Cada pantalla se descarga recién cuando se visita (code splitting).

## Estructura

```
src/
 ├─ api/          cliente HTTP (token + renovación automática), endpoints tipados, contratos (DTO)
 ├─ auth/         sesión, AuthProvider, useAuth, rutas protegidas
 ├─ comun/        componentes compartidos, RUT, formatos, consultas de catálogos
 ├─ layout/       barra lateral (menú según rol) y barra superior
 ├─ paginas/      una carpeta por módulo: empleados, vacaciones, organizacion, seguros, administracion, cuenta
 └─ pruebas/      pruebas unitarias
```

## Pantallas por rol

| Pantalla | Admin | RRHH | Jefatura | Empleado |
|---|:-:|:-:|:-:|:-:|
| Inicio (indicadores y dotación por región/departamento) | ✔ | ✔ (sus regiones) | saldo propio | saldo propio |
| Empleados: búsqueda, ficha, crear, editar, desvincular, Excel | ✔ | ✔ (sus regiones) | ver su equipo | su ficha |
| Mis vacaciones (saldo, solicitar, cancelar) | ✔ | ✔ | ✔ | ✔ |
| Aprobaciones de su equipo | ✔ | ✔ | ✔ | — |
| Departamentos y cargos, planes de seguro, feriados | ✔ | ✔ | — | — |
| Usuarios y auditoría | ✔ | — | — | — |
| Mi cuenta (cambiar clave) | ✔ | ✔ | ✔ | ✔ |

## Seguridad en el cliente

- **Token de acceso solo en memoria**; el token de renovación en `sessionStorage` (se borra al cerrar la pestaña).
- Ante un 401, el cliente renueva el token **una sola vez** (aunque haya varias peticiones en paralelo) y repite la petición;
  si la renovación falla, cierra la sesión y vuelve al login.
- El Excel se descarga con el token en la cabecera (nunca en la URL).
- El menú y las rutas se ocultan según el rol, pero **la seguridad real está en la API**: la interfaz solo evita mostrar lo que no corresponde.

## Ejecutar

Requiere Node.js 20 o superior y la API corriendo en `https://localhost:7052`.

```powershell
cd frontend
npm install
npm run dev          # http://localhost:5173 (las llamadas /api se reenvían a la API)
```

Usuarios demo: ver el README principal (clave `Demo.Rrhh2026`). En modo desarrollo la pantalla de login los lista.

Otros comandos:

```powershell
npm test             # pruebas unitarias
npm run build        # compila TypeScript y genera dist/
npm run lint         # oxlint
```

Para apuntar a otra URL de API en desarrollo: `$env:RRHH_API_URL="https://localhost:7052"; npm run dev`.
