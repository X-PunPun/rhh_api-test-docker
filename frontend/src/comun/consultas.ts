import { useQuery } from '@tanstack/react-query'
import { organizacionApi, ubicacionApi } from '../api/endpoints'

/** Catálogos compartidos por varias pantallas (se cachean 10 minutos). */
const DIEZ_MINUTOS = 10 * 60 * 1000

export function useRegiones() {
  return useQuery({ queryKey: ['regiones'], queryFn: ubicacionApi.regiones, staleTime: DIEZ_MINUTOS })
}

export function useComunas(regionId: number | undefined) {
  return useQuery({
    queryKey: ['comunas', regionId],
    queryFn: () => ubicacionApi.comunas(regionId!),
    enabled: regionId !== undefined,
    staleTime: DIEZ_MINUTOS,
  })
}

export function useDepartamentos(activo?: boolean) {
  return useQuery({ queryKey: ['departamentos', activo], queryFn: () => organizacionApi.departamentos(activo) })
}

export function useCargos(departamentoId?: number, activo?: boolean) {
  return useQuery({
    queryKey: ['cargos', departamentoId, activo],
    queryFn: () => organizacionApi.cargos(departamentoId, activo),
  })
}
