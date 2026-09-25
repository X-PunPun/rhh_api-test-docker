import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { renovarSesion } from '../api/cliente'
import { authApi } from '../api/endpoints'
import { EVENTO_SESION_EXPIRADA, sesion } from '../api/sesion'
import type { Perfil } from '../api/tipos'
import { Contexto, type ContextoAuth } from './contexto'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [usuario, setUsuario] = useState<Perfil | null>(null)
  const [cargando, setCargando] = useState(true)

  // Al abrir o recargar la página: si hay token de renovación, se recupera la sesión.
  useEffect(() => {
    let vigente = true
    ;(async () => {
      if (sesion.tokenRenovacion && (await renovarSesion())) {
        if (vigente) setUsuario(sesion.perfil)
      } else {
        sesion.limpiar()
      }
      if (vigente) setCargando(false)
    })()
    return () => {
      vigente = false
    }
  }, [])

  // Si la API rechaza la renovación en cualquier momento, se vuelve al login.
  useEffect(() => {
    const alExpirar = () => {
      setUsuario(null)
      queryClient.clear()
    }
    window.addEventListener(EVENTO_SESION_EXPIRADA, alExpirar)
    return () => window.removeEventListener(EVENTO_SESION_EXPIRADA, alExpirar)
  }, [queryClient])

  const iniciarSesion = useCallback(async (email: string, clave: string) => {
    const tokens = await authApi.login(email, clave)
    sesion.guardar(tokens)
    queryClient.clear()
    setUsuario(tokens.usuario)
  }, [queryClient])

  const cerrarSesion = useCallback(async () => {
    const token = sesion.tokenRenovacion
    sesion.limpiar()
    setUsuario(null)
    queryClient.clear()
    if (token) await authApi.logout(token).catch(() => undefined)
  }, [queryClient])

  const valor = useMemo<ContextoAuth>(() => ({
    usuario,
    cargando,
    iniciarSesion,
    cerrarSesion,
    tieneRol: (...roles) => usuario !== null && roles.includes(usuario.rol),
    esGestor: usuario?.rol === 'Admin' || usuario?.rol === 'RRHH',
  }), [usuario, cargando, iniciarSesion, cerrarSesion])

  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>
}

