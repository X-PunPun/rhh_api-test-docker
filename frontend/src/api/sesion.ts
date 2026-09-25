import type { Perfil, Tokens } from './tipos'

/**
 * Dónde vive cada credencial:
 * - Token de acceso (15 min): SOLO en memoria. Un script inyectado no lo encuentra en el almacenamiento.
 * - Token de renovación: sessionStorage (se borra al cerrar la pestaña) para sobrevivir a un F5.
 *   Mejora futura: moverlo a una cookie HttpOnly emitida por la API.
 */
const CLAVE_RENOVACION = 'rrhh.renovacion'
const CLAVE_PERFIL = 'rrhh.perfil'

let tokenAcceso: string | null = null

function leer(clave: string): string | null {
  try {
    return sessionStorage.getItem(clave)
  } catch {
    return null
  }
}

function escribir(clave: string, valor: string | null) {
  try {
    if (valor === null) sessionStorage.removeItem(clave)
    else sessionStorage.setItem(clave, valor)
  } catch {
    /* almacenamiento bloqueado: la sesión dura solo mientras la página esté abierta */
  }
}

export const sesion = {
  get tokenAcceso() {
    return tokenAcceso
  },
  get tokenRenovacion() {
    return leer(CLAVE_RENOVACION)
  },
  get perfil(): Perfil | null {
    const texto = leer(CLAVE_PERFIL)
    if (!texto) return null
    try {
      return JSON.parse(texto) as Perfil
    } catch {
      return null
    }
  },
  guardar(tokens: Tokens) {
    tokenAcceso = tokens.tokenAcceso
    escribir(CLAVE_RENOVACION, tokens.tokenRenovacion)
    escribir(CLAVE_PERFIL, JSON.stringify(tokens.usuario))
  },
  limpiar() {
    tokenAcceso = null
    escribir(CLAVE_RENOVACION, null)
    escribir(CLAVE_PERFIL, null)
  },
}

/** Evento que avisa a la interfaz que la sesión terminó (token de renovación inválido). */
export const EVENTO_SESION_EXPIRADA = 'rrhh:sesion-expirada'
