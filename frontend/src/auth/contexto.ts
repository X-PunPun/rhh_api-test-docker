import { createContext, useContext } from 'react'
import type { Perfil, Rol } from '../api/tipos'

export interface ContextoAuth {
  usuario: Perfil | null
  cargando: boolean
  iniciarSesion: (email: string, clave: string) => Promise<void>
  cerrarSesion: () => Promise<void>
  tieneRol: (...roles: Rol[]) => boolean
  esGestor: boolean
}

export const Contexto = createContext<ContextoAuth | null>(null)

/** Sesión actual: usuario, rol y acciones de login/logout. */
export function useAuth(): ContextoAuth {
  const contexto = useContext(Contexto)
  if (!contexto) throw new Error('useAuth debe usarse dentro de <AuthProvider>')
  return contexto
}
