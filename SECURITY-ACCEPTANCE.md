# Barrera de aceptación de seguridad

Criterios que **cada módulo debe cumplir antes de hacer merge a `main`**.
Basados en la metodología de [cloudflare/security-audit-skill](https://github.com/cloudflare/security-audit-skill)
(clases de ataque, aislamiento de datos, auth web) adaptados a un módulo interno de RR.HH.

> Regla de oro (Cloudflare): un hallazgo es real solo si alguien **cruza un límite de confianza**
> (ej.: un jefe ve empleados que no son suyos). "Falta un header" por sí solo es una nota de endurecimiento.

## Severidad

| Nivel | Ejemplo en este sistema |
|---|---|
| Crítica | Acceso sin autenticación a datos de empleados o toma de cuentas |
| Alta | Un usuario lee/modifica empleados fuera de su alcance (IDOR), escalamiento a Admin |
| Media | Exportación que incluye campos o registros fuera del alcance en casos acotados |
| Baja | Revelación de información interna no sensible (versiones, rutas) |
| Informativa | Observación sin impacto demostrable |

Hallazgos **Críticos o Altos bloquean el merge**.

## Checklist por Pull Request

### 1. Autenticación y tokens (WEB-PROTOCOL-AND-AUTH)
- [ ] Todo endpoint exige autenticación salvo `[AllowAnonymous]` explícito y justificado.
- [ ] JWT valida firma, `iss`, `aud` y `exp`; access token ≤ 15 min.
- [ ] Refresh token rotativo, guardado como hash, revocable (logout, cambio de clave, desvinculación).
- [ ] Bloqueo de cuenta tras intentos fallidos y rate limiting en `/login`.

### 2. Autorización y aislamiento de datos (ATTACK-CLASSES, DATA-ISOLATION)
- [ ] Cada consulta por Id verifica que el recurso está dentro del **alcance** del usuario
      (Empleado: solo él mismo · Jefatura: sus subordinados · RR.HH.: su(s) región(es) · Admin: todo).
- [ ] Listados, conteos, búsquedas y **exportaciones Excel** aplican el mismo filtro de alcance.
- [ ] Ningún campo del body (ej. `jefeId`, `rol`, `regionId`) puede ampliar el alcance del usuario.
- [ ] Un usuario no puede asignarse roles ni aprobar sus propias solicitudes.
- [ ] Existe **al menos una prueba de integración negativa** por endpoint (usuario fuera de alcance → 403/404).

### 3. Lógica de negocio
- [ ] Reglas en el dominio, no en controladores (ej.: fechas, saldos de vacaciones).
- [ ] Concurrencia optimista (`RowVersion`) en entidades que editan varias personas.
- [ ] Transiciones de estado válidas (Solicitud → Aprobada/Rechazada; no se re-aprueba).

### 4. Datos sensibles (Ley 21.719)
- [ ] DTOs exponen el mínimo necesario; salud/previsión/remuneraciones solo a roles autorizados.
- [ ] Datos personales y de salud **no** se escriben en logs.
- [ ] Acciones sensibles quedan en auditoría (quién, qué, cuándo).

### 5. Exportaciones
- [ ] Celdas que empiezan con `=`, `+`, `-`, `@` se neutralizan (inyección de fórmulas).
- [ ] Límite de filas/tamaño por exportación.

### 6. "Obvious things"
- [ ] Sin secretos en el repositorio (`appsettings*.json`, `.env`). Usar user-secrets / variables de entorno.
- [ ] Errores devuelven ProblemDetails sin stack trace.
- [ ] OpenAPI/Scalar solo en Development.
- [ ] CORS con orígenes explícitos, nunca `*` con credenciales.
- [ ] HTTPS + HSTS fuera de desarrollo.
- [ ] Dependencias sin vulnerabilidades conocidas: `dotnet list package --vulnerable`.

## Auditoría final

Al cerrar el backend se ejecuta la skill `security-audit` en modo *full audit*, perfil `quick`,
y sus resultados (`confirmed` / `needs_validation`) se registran en `docs/auditoria/`.
