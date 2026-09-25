import { EVENTO_SESION_EXPIRADA, sesion } from './sesion'
import type { ProblemDetails, Tokens } from './tipos'

const BASE = '/api/v1'

/** Error de la API con el ProblemDetails ya interpretado. */
export class ErrorApi extends Error {
  readonly estado: number
  readonly problema: ProblemDetails

  constructor(estado: number, problema: ProblemDetails) {
    super(mensajeDe(estado, problema))
    this.name = 'ErrorApi'
    this.estado = estado
    this.problema = problema
  }
}

function mensajeDe(estado: number, problema: ProblemDetails): string {
  if (problema.errors) {
    const primeros = Object.values(problema.errors).flat().slice(0, 3)
    if (primeros.length > 0) return primeros.join(' ')
  }
  if (problema.detail) return problema.detail
  if (problema.title) return problema.title
  if (estado === 403) return 'No tiene permisos para esta acción.'
  if (estado === 404) return 'El recurso no existe o no tiene acceso a él.'
  if (estado === 429) return 'Demasiadas solicitudes. Espere un minuto.'
  return `Error ${estado} al comunicarse con el servidor.`
}

async function leerProblema(respuesta: Response): Promise<ProblemDetails> {
  try {
    return (await respuesta.json()) as ProblemDetails
  } catch {
    return {}
  }
}

// Una sola renovación en curso: si 5 peticiones reciben 401 a la vez, se renueva una vez.
let renovacionEnCurso: Promise<boolean> | null = null

export function renovarSesion(): Promise<boolean> {
  renovacionEnCurso ??= (async () => {
    const tokenRenovacion = sesion.tokenRenovacion
    if (!tokenRenovacion) return false

    try {
      const respuesta = await fetch(`${BASE}/auth/renovar`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tokenRenovacion }),
      })
      if (!respuesta.ok) return false
      sesion.guardar((await respuesta.json()) as Tokens)
      return true
    } catch {
      return false
    }
  })().finally(() => {
    renovacionEnCurso = null
  })

  return renovacionEnCurso
}

type Metodo = 'GET' | 'POST' | 'PUT' | 'DELETE'

interface Opciones {
  metodo?: Metodo
  cuerpo?: unknown
  consulta?: object
  /** Para descargas (Excel): devuelve la Response sin interpretar. */
  crudo?: boolean
}

export function construirConsulta(parametros?: object): string {
  if (!parametros) return ''
  const qs = new URLSearchParams()
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== null && valor !== '') qs.set(clave, String(valor))
  }
  const texto = qs.toString()
  return texto ? `?${texto}` : ''
}

async function enviar(ruta: string, opciones: Opciones): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (opciones.cuerpo !== undefined) headers['Content-Type'] = 'application/json'
  if (sesion.tokenAcceso) headers.Authorization = `Bearer ${sesion.tokenAcceso}`

  return fetch(`${BASE}${ruta}${construirConsulta(opciones.consulta)}`, {
    method: opciones.metodo ?? 'GET',
    headers,
    body: opciones.cuerpo !== undefined ? JSON.stringify(opciones.cuerpo) : undefined,
  })
}

/**
 * Cliente HTTP de la aplicación: agrega el token, renueva la sesión ante un 401
 * (una vez) y convierte los errores en {@link ErrorApi}.
 */
export async function api<T>(ruta: string, opciones: Opciones = {}): Promise<T> {
  let respuesta = await enviar(ruta, opciones)

  if (respuesta.status === 401 && sesion.tokenRenovacion && !ruta.startsWith('/auth/')) {
    if (await renovarSesion()) {
      respuesta = await enviar(ruta, opciones)
    } else {
      sesion.limpiar()
      window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA))
    }
  }

  if (!respuesta.ok) {
    throw new ErrorApi(respuesta.status, await leerProblema(respuesta))
  }

  if (opciones.crudo) return respuesta as T
  if (respuesta.status === 204) return undefined as T
  return (await respuesta.json()) as T
}

/** Descarga un archivo protegido (el token va en la cabecera, no en la URL). */
export async function descargar(ruta: string, consulta?: object, nombrePorDefecto = 'archivo'): Promise<void> {
  const respuesta = await api<Response>(ruta, { consulta, crudo: true })
  const disposicion = respuesta.headers.get('Content-Disposition') ?? ''
  const nombre = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposicion)?.[1] ?? nombrePorDefecto

  const url = URL.createObjectURL(await respuesta.blob())
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = decodeURIComponent(nombre)
  enlace.click()
  URL.revokeObjectURL(url)
}
