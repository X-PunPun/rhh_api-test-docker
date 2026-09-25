import { api, construirConsulta, ErrorApi } from '../api/cliente'
import { EVENTO_SESION_EXPIRADA, sesion } from '../api/sesion'
import type { Tokens } from '../api/tipos'

function respuesta(estado: number, cuerpo?: unknown): Response {
  return new Response(cuerpo === undefined ? null : JSON.stringify(cuerpo), {
    status: estado,
    headers: { 'Content-Type': 'application/json' },
  })
}

const tokens = (acceso: string): Tokens => ({
  tokenAcceso: acceso,
  tokenAccesoExpiraEn: '',
  tokenRenovacion: `renovacion-${acceso}`,
  tokenRenovacionExpiraEn: '',
  usuario: { usuarioId: 1, email: 'a@b.cl', rol: 'Empleado', empleadoId: 7, nombre: 'Ana', regiones: [] },
})

describe('cliente HTTP', () => {
  beforeEach(() => {
    sesion.limpiar()
    vi.restoreAllMocks()
  })

  it('envía el token y devuelve el JSON', async () => {
    sesion.guardar(tokens('A'))
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(respuesta(200, { ok: true }))

    await expect(api('/regiones')).resolves.toEqual({ ok: true })
    const init = fetchMock.mock.calls[0]![1]!
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer A')
  })

  it('ante un 401 renueva el token una vez y repite la petición', async () => {
    sesion.guardar(tokens('A'))
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(respuesta(401))
      .mockResolvedValueOnce(respuesta(200, tokens('B')))
      .mockResolvedValueOnce(respuesta(200, [1, 2]))

    await expect(api('/empleados')).resolves.toEqual([1, 2])
    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(String(fetchMock.mock.calls[1]![0])).toBe('/api/v1/auth/renovar')
    expect(sesion.tokenAcceso).toBe('B')
  })

  it('si la renovación falla, limpia la sesión y avisa a la interfaz', async () => {
    sesion.guardar(tokens('A'))
    vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(respuesta(401))
      .mockResolvedValueOnce(respuesta(401))
    const escucha = vi.fn()
    window.addEventListener(EVENTO_SESION_EXPIRADA, escucha)

    await expect(api('/empleados')).rejects.toBeInstanceOf(ErrorApi)
    expect(sesion.tokenRenovacion).toBeNull()
    expect(escucha).toHaveBeenCalledOnce()
    window.removeEventListener(EVENTO_SESION_EXPIRADA, escucha)
  })

  it('convierte ProblemDetails en un mensaje legible', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      respuesta(409, { title: 'Conflicto', status: 409, detail: 'Saldo insuficiente.' }))

    await expect(api('/x')).rejects.toThrow('Saldo insuficiente.')
  })

  it('omite parámetros vacíos en la consulta', () => {
    expect(construirConsulta({ regionId: 13, busqueda: '', activo: undefined, pagina: 1 })).toBe('?regionId=13&pagina=1')
  })
})
