import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router'
import { vacacionesApi } from '../../api/endpoints'
import type { SolicitudVacaciones } from '../../api/tipos'
import { Aviso, Campo, Cargando, Dialogo, EncabezadoPagina, Vacio } from '../../comun/componentes/Basicos'
import { fecha, fechaYHora, mensajeError } from '../../comun/formato'

/** Bandeja de la jefatura: solicitudes pendientes de su equipo directo. */
export function Aprobaciones() {
  const queryClient = useQueryClient()
  const [rechazando, setRechazando] = useState<SolicitudVacaciones | null>(null)
  const pendientes = useQuery({ queryKey: ['pendientes-equipo'], queryFn: vacacionesApi.pendientesEquipo })

  const refrescar = () => queryClient.invalidateQueries({ queryKey: ['pendientes-equipo'] })
  const aprobar = useMutation({ mutationFn: vacacionesApi.aprobar, onSuccess: refrescar })

  return (
    <>
      <EncabezadoPagina titulo="Aprobaciones" descripcion="Solicitudes de vacaciones de su equipo directo pendientes de respuesta." />
      <Aviso>{aprobar.error && mensajeError(aprobar.error)}</Aviso>
      {pendientes.isLoading && <Cargando />}
      {pendientes.error && <Aviso>{mensajeError(pendientes.error)}</Aviso>}
      {pendientes.data && (pendientes.data.length === 0 ? <Vacio>No hay solicitudes pendientes.</Vacio> : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead><tr><th>Empleado</th><th>Período</th><th>Días hábiles</th><th>Comentario</th><th>Solicitada</th><th /></tr></thead>
            <tbody>
              {pendientes.data.map((s) => (
                <tr key={s.id}>
                  <td><Link to={`/empleados/${s.empleadoId}`}>{s.empleado}</Link></td>
                  <td className="mono">{fecha(s.fechaInicio)} → {fecha(s.fechaFin)}</td>
                  <td>{s.diasHabiles}</td>
                  <td>{s.comentario ?? '—'}</td>
                  <td className="mono">{fechaYHora(s.fechaSolicitud)}</td>
                  <td className="acciones-fila">
                    <button className="boton boton-primario" disabled={aprobar.isPending} onClick={() => aprobar.mutate(s.id)}>Aprobar</button>
                    <button className="boton boton-secundario" onClick={() => setRechazando(s)}>Rechazar</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
      <DialogoRechazo solicitud={rechazando} onCerrar={() => setRechazando(null)} onRechazada={refrescar} />
    </>
  )
}

function DialogoRechazo({ solicitud, onCerrar, onRechazada }: {
  solicitud: SolicitudVacaciones | null
  onCerrar: () => void
  onRechazada: () => Promise<unknown>
}) {
  const [motivo, setMotivo] = useState('')
  const rechazar = useMutation({
    mutationFn: () => vacacionesApi.rechazar(solicitud!.id, motivo.trim()),
    onSuccess: async () => {
      setMotivo('')
      await onRechazada()
      onCerrar()
    },
  })

  return (
    <Dialogo abierto={solicitud !== null} titulo={`Rechazar solicitud de ${solicitud?.empleado ?? ''}`} onCerrar={onCerrar}>
      <form className="formulario" onSubmit={(e) => { e.preventDefault(); rechazar.mutate() }}>
        <Campo etiqueta="Motivo (lo verá el empleado)" id="motivo-rechazo">
          <textarea id="motivo-rechazo" required maxLength={500} rows={3} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
        </Campo>
        <Aviso>{rechazar.error && mensajeError(rechazar.error)}</Aviso>
        <div className="acciones">
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>Cancelar</button>
          <button className="boton boton-peligro" disabled={rechazar.isPending || motivo.trim() === ''}>Rechazar</button>
        </div>
      </form>
    </Dialogo>
  )
}
