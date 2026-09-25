import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import type { Rol } from '../api/tipos'
import { useAuth } from './contexto'

/**
 * Protege rutas en la interfaz. Es solo comodidad de navegación:
 * la seguridad real la aplica la API en cada petición.
 */
export function RutaProtegida({ roles, children }: { roles?: Rol[]; children: ReactNode }) {
  const { usuario, cargando } = useAuth()
  const ubicacion = useLocation()

  if (cargando) return <p className="cargando-pagina">Cargando sesión…</p>
  if (!usuario) return <Navigate to="/login" replace state={{ desde: ubicacion.pathname }} />
  if (roles && !roles.includes(usuario.rol)) return <Navigate to="/" replace />

  return children
}
