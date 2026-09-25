import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { vacacionesApi } from '../../api/endpoints'
import { useAuth } from '../../auth/contexto'
import { Aviso, Campo, Cargando, EncabezadoPagina, Indicador, PillEstado, Vacio } from '../../comun/componentes/Basicos'
import { fecha, hoyIso, mensajeError, num } from '../../comun/formato'
import { contarDiasHabiles } from '../../comun/vacaciones'

export function MisVacaciones() {
  const { usuario } = useAuth()
  const empleadoId = usuario?.empleadoId

  if (!empleadoId) {
    return (
      <>
        <EncabezadoPagina titulo="Mis vacaciones" />
        <Vacio>Su usuario no está asociado a un empleado.</Vacio>
      </>
    )
  }

  return <Vacaciones empleadoId={empleadoId} />
}

function Vacaciones({ empleadoId }: { empleadoId: number }) {
  const queryClient = useQueryClient()
  const [inicio, setInicio] = useState('')
  const [fin, setFin] = useState('')
  const [comentario, setComentario] = useState('')
  const [exito, setExito] = useState<string | null>(null)

  const saldo = useQuery({ queryKey: ['saldo', empleadoId], queryFn: () => vacacionesApi.saldo(empleadoId) })
  const historial = useQuery({ queryKey: ['vacaciones', empleadoId], queryFn: () => vacacionesApi.historial(empleadoId) })
  const anio = inicio ? Number(inicio.slice(0, 4)) : new Date().getFullYear()
  const feriados = useQuery({ queryKey: ['feriados', anio], queryFn: () => vacacionesApi.feriados(anio) })

  const fechasFeriado = useMemo(() => new Set(feriados.data?.map((f) => f.fecha)), [feriados.data])
  const diasPrevistos = contarDiasHabiles(inicio, fin, fechasFeriado)

  const refrescar = () => Promise.all([
    queryClient.invalidateQueries({ queryKey: ['saldo', empleadoId] }),
    queryClient.invalidateQueries({ queryKey: ['vacaciones', empleadoId] }),
  ])

  const solicitar = useMutation({
    mutationFn: () => vacacionesApi.solicitar(empleadoId, inicio, fin, comentario.trim() || null),
    onSuccess: async (s) => {
      setExito(`Solicitud enviada: ${s.diasHabiles} día(s) hábil(es). Queda pendiente de aprobación de su jefatura.`)
      setInicio('')
      setFin('')
      setComentario('')
      await refrescar()
    },
  })

  const cancelar = useMutation({ mutationFn: vacacionesApi.cancelar, onSuccess: refrescar })

  return (
    <>
      <EncabezadoPagina titulo="Mis vacaciones" descripcion="Feriado legal: 15 días hábiles por año trabajado (1,25 por mes), más feriado progresivo." />

      {saldo.data && (
        <div className="indicadores">
          <Indicador etiqueta="Disponibles" valor={num(saldo.data.diasDisponibles)} detalle="días hábiles" />
          <Indicador etiqueta="Devengados" valor={num(saldo.data.diasDevengados)} />
          <Indicador etiqueta="Tomados" valor={saldo.data.diasTomados} />
          <Indicador etiqueta="Pendientes" valor={saldo.data.diasPendientesAprobacion} />
        </div>
      )}

      <section className="tarjeta">
        <h2>Nueva solicitud</h2>
        <form className="formulario" onSubmit={(e) => { e.preventDefault(); setExito(null); solicitar.mutate() }}>
          <div className="grilla-campos">
            <Campo etiqueta="Desde" id="vac-inicio">
              <input id="vac-inicio" type="date" required min={hoyIso()} value={inicio} onChange={(e) => setInicio(e.target.value)} />
            </Campo>
            <Campo etiqueta="Hasta" id="vac-fin">
              <input id="vac-fin" type="date" required min={inicio || hoyIso()} value={fin} onChange={(e) => setFin(e.target.value)} />
            </Campo>
            <Campo etiqueta="Comentario (opcional)" id="vac-comentario">
              <input id="vac-comentario" maxLength={500} value={comentario} onChange={(e) => setComentario(e.target.value)} />
            </Campo>
          </div>
          {inicio && fin && (
            <p className="texto-secundario">
              Se descontarán <strong>{diasPrevistos}</strong> día(s) hábil(es) (sin fines de semana ni feriados).
            </p>
          )}
          <Aviso>{solicitar.error && mensajeError(solicitar.error)}</Aviso>
          <Aviso tipo="exito">{exito}</Aviso>
          <div className="acciones">
            <button className="boton boton-primario" disabled={solicitar.isPending || diasPrevistos === 0}>Enviar solicitud</button>
          </div>
        </form>
      </section>

      <section className="seccion">
        <h2>Historial</h2>
        <Aviso>{cancelar.error && mensajeError(cancelar.error)}</Aviso>
        {historial.isLoading && <Cargando />}
        {historial.data && (historial.data.length === 0 ? <Vacio>Aún no tiene solicitudes.</Vacio> : (
          <div className="tabla-contenedor">
            <table className="tabla">
              <thead><tr><th>Período</th><th>Días</th><th>Estado</th><th>Detalle</th><th /></tr></thead>
              <tbody>
                {historial.data.map((s) => (
                  <tr key={s.id}>
                    <td className="mono">{fecha(s.fechaInicio)} → {fecha(s.fechaFin)}</td>
                    <td>{s.diasHabiles}</td>
                    <td><PillEstado estado={s.estado} /></td>
                    <td>{s.motivoRechazo ? `Motivo: ${s.motivoRechazo}` : s.comentario ?? '—'}</td>
                    <td>
                      {s.estado === 'Pendiente' && (
                        <button className="boton boton-texto" disabled={cancelar.isPending} onClick={() => cancelar.mutate(s.id)}>
                          Cancelar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
      </section>
    </>
  )
}
