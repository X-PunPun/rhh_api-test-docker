// Contratos de la API (espejo de los DTO de RRHH.Application). Las fechas llegan como texto ISO.

export type Rol = 'Admin' | 'RRHH' | 'Jefatura' | 'Empleado'
export type Afp = 'Capital' | 'Cuprum' | 'Habitat' | 'Modelo' | 'PlanVital' | 'Provida' | 'Uno'
export type SistemaSalud = 'Fonasa' | 'Isapre'
export type EstadoSolicitud = 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Cancelada'
export type TipoSeguro = 'Salud' | 'Vida' | 'Dental' | 'Catastrofico'

export const AFPS = ['Capital', 'Cuprum', 'Habitat', 'Modelo', 'PlanVital', 'Provida', 'Uno'] as const satisfies readonly Afp[]
export const SISTEMAS_SALUD = ['Fonasa', 'Isapre'] as const satisfies readonly SistemaSalud[]
export const ROLES = ['Admin', 'RRHH', 'Jefatura', 'Empleado'] as const satisfies readonly Rol[]
export const TIPOS_SEGURO = ['Salud', 'Vida', 'Dental', 'Catastrofico'] as const satisfies readonly TipoSeguro[]

/** Texto para mostrar (el valor de la API no lleva tilde porque es el nombre del enum). */
export const ETIQUETA_TIPO_SEGURO: Record<TipoSeguro, string> = {
  Salud: 'Salud',
  Vida: 'Vida',
  Dental: 'Dental',
  Catastrofico: 'Catastrófico',
}

export interface Referencia { id: number; nombre: string }

export interface Pagina<T> {
  items: T[]
  numeroPagina: number
  tamanoPagina: number
  total: number
  totalPaginas: number
}

// ---------- Seguridad ----------
export interface Perfil {
  usuarioId: number
  email: string
  rol: Rol
  empleadoId: number | null
  nombre: string | null
  regiones: number[]
}

export interface Tokens {
  tokenAcceso: string
  tokenAccesoExpiraEn: string
  tokenRenovacion: string
  tokenRenovacionExpiraEn: string
  usuario: Perfil
}

export interface Usuario {
  id: number
  email: string
  rol: Rol
  empleadoId: number | null
  empleado: string | null
  regiones: number[]
  activo: boolean
  bloqueado: boolean
  ultimoAcceso: string | null
}

export interface CrearUsuario {
  empleadoId: number | null
  email: string | null
  rol: Rol
  regiones: number[]
  claveInicial: string
}

export interface RegistroAuditoria {
  id: number
  fecha: string
  usuarioId: number | null
  email: string | null
  accion: string
  detalle: string | null
  codigoResultado: number | null
  ip: string | null
}

// ---------- Ubicación ----------
export interface Region { id: number; nombre: string; abreviatura: string }
export interface Comuna { id: number; nombre: string; regionId: number }

// ---------- Organización ----------
export interface Departamento {
  id: number
  nombre: string
  descripcion: string | null
  activo: boolean
  empleadosActivos: number
}

export interface Cargo {
  id: number
  nombre: string
  departamentoId: number
  departamento: string
  activo: boolean
}

// ---------- Empleados ----------
export interface EmpleadoResumen {
  id: number
  rut: string
  nombreCompleto: string
  email: string
  cargo: string
  departamento: string
  region: string
  comuna: string
  jefe: string | null
  fechaIngreso: string
  activo: boolean
}

export interface EmpleadoDetalle {
  id: number
  rut: string
  nombres: string
  apellidoPaterno: string
  apellidoMaterno: string | null
  email: string
  fechaNacimiento: string
  fechaIngreso: string
  fechaTermino: string | null
  activo: boolean
  departamento: Referencia
  cargo: Referencia
  region: Referencia
  comuna: Referencia
  jefe: Referencia | null
  afp: Afp | null
  sistemaSalud: SistemaSalud | null
  aniosServicioPrevios: number | null
  cantidadSubordinados: number
}

export interface FiltroEmpleados {
  regionId?: number
  comunaId?: number
  departamentoId?: number
  cargoId?: number
  jefeId?: number
  activo?: boolean
  busqueda?: string
  pagina?: number
  tamanoPagina?: number
}

export interface GuardarEmpleado {
  rut: string
  nombres: string
  apellidoPaterno: string
  apellidoMaterno: string | null
  email: string
  fechaNacimiento: string
  fechaIngreso: string
  departamentoId: number
  cargoId: number
  comunaId: number
  jefeId: number | null
  afp: Afp
  sistemaSalud: SistemaSalud
  aniosServicioPrevios: number
}

// ---------- Vacaciones ----------
export interface SaldoVacaciones {
  empleadoId: number
  fechaCalculo: string
  aniosServicio: number
  diasProgresivosAnuales: number
  diasDevengados: number
  diasTomados: number
  diasPendientesAprobacion: number
  diasDisponibles: number
}

export interface SolicitudVacaciones {
  id: number
  empleadoId: number
  empleado: string
  fechaInicio: string
  fechaFin: string
  diasHabiles: number
  estado: EstadoSolicitud
  comentario: string | null
  fechaSolicitud: string
  resueltaPorId: number | null
  fechaResolucion: string | null
  motivoRechazo: string | null
}

export interface Feriado { id: number; fecha: string; nombre: string }

// ---------- Seguros ----------
export interface PlanSeguro {
  id: number
  nombre: string
  aseguradora: string
  tipo: TipoSeguro
  primaMensualUf: number
  activo: boolean
}

export interface AfiliacionSeguro {
  id: number
  empleadoId: number
  planSeguroId: number
  plan: string
  aseguradora: string
  tipo: TipoSeguro
  primaMensualUf: number
  fechaInicio: string
  fechaTermino: string | null
  numeroCargas: number
  vigente: boolean
}

// ---------- Reportes ----------
export interface Conteo { id: number; nombre: string; cantidad: number }

export interface Resumen {
  fecha: string
  empleadosActivos: number
  empleadosDesvinculados: number
  solicitudesVacacionesPendientes: number
  afiliacionesSeguroVigentes: number
  empleadosPorRegion: Conteo[]
  empleadosPorDepartamento: Conteo[]
}

/** Error estándar de la API (ProblemDetails, RFC 9457). */
export interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}
