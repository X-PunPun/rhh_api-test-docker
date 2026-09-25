import { api, descargar } from './cliente'
import type {
  AfiliacionSeguro, Cargo, Comuna, CrearUsuario, Departamento, EmpleadoDetalle, EmpleadoResumen, Feriado,
  FiltroEmpleados, GuardarEmpleado, Pagina, Perfil, PlanSeguro, RegistroAuditoria, Region, Resumen, Rol,
  SaldoVacaciones, SolicitudVacaciones, TipoSeguro, Tokens, Usuario, EstadoSolicitud,
} from './tipos'

// Una función por endpoint: la interfaz nunca arma URLs a mano.

export const authApi = {
  login: (email: string, clave: string) => api<Tokens>('/auth/login', { metodo: 'POST', cuerpo: { email, clave } }),
  logout: (tokenRenovacion: string) => api<void>('/auth/logout', { metodo: 'POST', cuerpo: { tokenRenovacion } }),
  yo: () => api<Perfil>('/auth/yo'),
  cambiarClave: (claveActual: string, claveNueva: string) =>
    api<void>('/auth/cambiar-clave', { metodo: 'POST', cuerpo: { claveActual, claveNueva } }),
}

export const ubicacionApi = {
  regiones: () => api<Region[]>('/regiones'),
  comunas: (regionId: number) => api<Comuna[]>(`/regiones/${regionId}/comunas`),
}

export const organizacionApi = {
  departamentos: (activo?: boolean) => api<Departamento[]>('/departamentos', { consulta: { activo } }),
  crearDepartamento: (nombre: string, descripcion: string | null) =>
    api<Departamento>('/departamentos', { metodo: 'POST', cuerpo: { nombre, descripcion } }),
  actualizarDepartamento: (id: number, nombre: string, descripcion: string | null) =>
    api<Departamento>(`/departamentos/${id}`, { metodo: 'PUT', cuerpo: { nombre, descripcion } }),
  estadoDepartamento: (id: number, activo: boolean) =>
    api<void>(`/departamentos/${id}/${activo ? 'activar' : 'desactivar'}`, { metodo: 'POST' }),
  cargos: (departamentoId?: number, activo?: boolean) => api<Cargo[]>('/cargos', { consulta: { departamentoId, activo } }),
  crearCargo: (nombre: string, departamentoId: number) =>
    api<Cargo>('/cargos', { metodo: 'POST', cuerpo: { nombre, departamentoId } }),
  renombrarCargo: (id: number, nombre: string) => api<Cargo>(`/cargos/${id}`, { metodo: 'PUT', cuerpo: { nombre } }),
  desactivarCargo: (id: number) => api<void>(`/cargos/${id}/desactivar`, { metodo: 'POST' }),
}

export const empleadosApi = {
  buscar: (filtro: FiltroEmpleados) => api<Pagina<EmpleadoResumen>>('/empleados', { consulta: filtro }),
  obtener: (id: number) => api<EmpleadoDetalle>(`/empleados/${id}`),
  subordinados: (id: number) => api<EmpleadoResumen[]>(`/empleados/${id}/subordinados`),
  crear: (datos: GuardarEmpleado) => api<EmpleadoDetalle>('/empleados', { metodo: 'POST', cuerpo: datos }),
  actualizar: (id: number, datos: Omit<GuardarEmpleado, 'rut' | 'nombres' | 'apellidoPaterno' | 'apellidoMaterno' | 'fechaNacimiento' | 'fechaIngreso'>) =>
    api<EmpleadoDetalle>(`/empleados/${id}`, { metodo: 'PUT', cuerpo: datos }),
  desvincular: (id: number, fechaTermino: string) =>
    api<void>(`/empleados/${id}/desvincular`, { metodo: 'POST', cuerpo: { fechaTermino } }),
  exportarExcel: (filtro: FiltroEmpleados) => descargar('/reportes/empleados/excel', filtro, 'empleados.xlsx'),
}

export const vacacionesApi = {
  saldo: (empleadoId: number) => api<SaldoVacaciones>(`/empleados/${empleadoId}/vacaciones/saldo`),
  historial: (empleadoId: number, estado?: EstadoSolicitud) =>
    api<SolicitudVacaciones[]>(`/empleados/${empleadoId}/vacaciones`, { consulta: { estado } }),
  solicitar: (empleadoId: number, fechaInicio: string, fechaFin: string, comentario: string | null) =>
    api<SolicitudVacaciones>(`/empleados/${empleadoId}/vacaciones`, { metodo: 'POST', cuerpo: { fechaInicio, fechaFin, comentario } }),
  pendientesEquipo: () => api<SolicitudVacaciones[]>('/vacaciones/pendientes-equipo'),
  aprobar: (id: number) => api<SolicitudVacaciones>(`/vacaciones/${id}/aprobar`, { metodo: 'POST' }),
  rechazar: (id: number, motivo: string) =>
    api<SolicitudVacaciones>(`/vacaciones/${id}/rechazar`, { metodo: 'POST', cuerpo: { motivo } }),
  cancelar: (id: number) => api<SolicitudVacaciones>(`/vacaciones/${id}/cancelar`, { metodo: 'POST' }),
  feriados: (anio: number) => api<Feriado[]>('/feriados', { consulta: { anio } }),
  crearFeriado: (fecha: string, nombre: string) => api<Feriado>('/feriados', { metodo: 'POST', cuerpo: { fecha, nombre } }),
  eliminarFeriado: (id: number) => api<void>(`/feriados/${id}`, { metodo: 'DELETE' }),
}

export const segurosApi = {
  planes: (activo?: boolean) => api<PlanSeguro[]>('/planes-seguro', { consulta: { activo } }),
  crearPlan: (datos: { nombre: string; aseguradora: string; tipo: TipoSeguro; primaMensualUf: number }) =>
    api<PlanSeguro>('/planes-seguro', { metodo: 'POST', cuerpo: datos }),
  estadoPlan: (id: number, activo: boolean) =>
    api<void>(`/planes-seguro/${id}/${activo ? 'activar' : 'desactivar'}`, { metodo: 'POST' }),
  afiliaciones: (empleadoId: number) => api<AfiliacionSeguro[]>(`/empleados/${empleadoId}/seguros`),
  afiliar: (empleadoId: number, planSeguroId: number, fechaInicio: string, numeroCargas: number) =>
    api<AfiliacionSeguro>(`/empleados/${empleadoId}/seguros`, { metodo: 'POST', cuerpo: { planSeguroId, fechaInicio, numeroCargas } }),
  terminar: (empleadoId: number, afiliacionId: number, fechaTermino: string) =>
    api<AfiliacionSeguro>(`/empleados/${empleadoId}/seguros/${afiliacionId}/terminar`, { metodo: 'POST', cuerpo: { fechaTermino } }),
}

export const reportesApi = {
  resumen: () => api<Resumen>('/reportes/resumen'),
}

export const usuariosApi = {
  listar: () => api<Usuario[]>('/usuarios'),
  crear: (datos: CrearUsuario) => api<Usuario>('/usuarios', { metodo: 'POST', cuerpo: datos }),
  asignarRol: (id: number, rol: Rol, regiones: number[]) =>
    api<Usuario>(`/usuarios/${id}/rol`, { metodo: 'PUT', cuerpo: { rol, regiones } }),
  estado: (id: number, activo: boolean) => api<void>(`/usuarios/${id}/${activo ? 'activar' : 'desactivar'}`, { metodo: 'POST' }),
  desbloquear: (id: number) => api<void>(`/usuarios/${id}/desbloquear`, { metodo: 'POST' }),
  restablecerClave: (id: number, claveNueva: string) =>
    api<void>(`/usuarios/${id}/restablecer-clave`, { metodo: 'POST', cuerpo: { claveNueva } }),
  auditoria: (filtro: { accion?: string; pagina?: number; tamanoPagina?: number }) =>
    api<Pagina<RegistroAuditoria>>('/auditoria', { consulta: filtro }),
}
