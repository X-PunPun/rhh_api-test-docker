import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { empleadosApi, segurosApi, vacacionesApi } from '../../api/endpoints'
import { ETIQUETA_TIPO_SEGURO, type EmpleadoDetalle } from '../../api/tipos'
import { useAuth } from '../../auth/contexto'
import { Aviso, Campo, Cargando, Dialogo, EncabezadoPagina, PillActivo, PillEstado, Vacio } from '../../comun/componentes/Basicos'
import { fecha, formatoUf, hoyIso, mensajeError, num } from '../../comun/formato'

export function DetalleEmpleado() {
  const id = Number(useParams().id)
  const { esGestor } = useAuth()
  const [desvinculando, setDesvinculando] = useState(false)

  const { data: e, isLoading, error } = useQuery({ queryKey: ['empleado', id], queryFn: () => empleadosApi.obtener(id) })

  if (isLoading) return <Cargando />
  if (error) return <Aviso>{mensajeError(error)}</Aviso>
  if (!e) return null

  return (
    <>
      <EncabezadoPagina
        titulo={[e.nombres, e.apellidoPaterno, e.apellidoMaterno].filter(Boolean).join(' ')}
        descripcion={`${e.cargo.nombre} · ${e.departamento.nombre}`}
        acciones={esGestor && e.activo && (
          <>
            <Link className="boton boton-secundario" to={`/empleados/${e.id}/editar`}>Editar</Link>
            <button className="boton boton-peligro" onClick={() => setDesvinculando(true)}>Desvincular</button>
          </>
        )}
      />

      <section className="tarjeta ficha">
        <Dato etiqueta="RUT" valor={<span className="mono">{e.rut}</span>} />
        <Dato etiqueta="Estado" valor={<PillActivo activo={e.activo} textoInactivo={`Desvinculado el ${fecha(e.fechaTermino)}`} />} />
        <Dato etiqueta="Email" valor={e.email} />
        <Dato etiqueta="Fecha de nacimiento" valor={fecha(e.fechaNacimiento)} />
        <Dato etiqueta="Fecha de ingreso" valor={fecha(e.fechaIngreso)} />
        <Dato etiqueta="Región" valor={e.region.nombre} />
        <Dato etiqueta="Comuna" valor={e.comuna.nombre} />
        <Dato etiqueta="Jefatura directa" valor={e.jefe ? <Link to={`/empleados/${e.jefe.id}`}>{e.jefe.nombre}</Link> : '—'} />
        {e.afp !== null ? (
          <>
            <Dato etiqueta="AFP" valor={e.afp} />
            <Dato etiqueta="Salud" valor={e.sistemaSalud ?? '—'} />
            <Dato etiqueta="Años con empleadores anteriores" valor={e.aniosServicioPrevios ?? '—'} />
          </>
        ) : (
          <Dato etiqueta="Previsión" valor={<span className="texto-secundario">Reservada (solo RR.HH. y el propio empleado)</span>} />
        )}
      </section>

      {e.cantidadSubordinados > 0 && <Equipo id={e.id} />}
      <Vacaciones id={e.id} />
      <Seguros empleado={e} puedeGestionar={esGestor} />

      <DialogoDesvincular empleado={e} abierto={desvinculando} onCerrar={() => setDesvinculando(false)} />
    </>
  )
}

function Dato({ etiqueta, valor }: { etiqueta: string; valor: ReactNode }) {
  return (
    <div className="dato">
      <span className="dato-etiqueta">{etiqueta}</span>
      <span>{valor}</span>
    </div>
  )
}

function Equipo({ id }: { id: number }) {
  const { data, isLoading } = useQuery({ queryKey: ['subordinados', id], queryFn: () => empleadosApi.subordinados(id) })
  return (
    <section className="seccion">
      <h2>Equipo directo</h2>
      {isLoading && <Cargando />}
      {data && (
        <ul className="lista-simple">
          {data.map((s) => (
            <li key={s.id}>
              <Link to={`/empleados/${s.id}`}>{s.nombreCompleto}</Link>
              <span className="texto-secundario"> · {s.cargo}</span>
              {!s.activo && <> <PillActivo activo={false} textoInactivo="Desvinculado" /></>}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function Vacaciones({ id }: { id: number }) {
  const saldo = useQuery({ queryKey: ['saldo', id], queryFn: () => vacacionesApi.saldo(id) })
  const historial = useQuery({ queryKey: ['vacaciones', id], queryFn: () => vacacionesApi.historial(id) })

  return (
    <section className="seccion">
      <h2>Vacaciones</h2>
      {saldo.data && (
        <p>
          <strong>{num(saldo.data.diasDisponibles)}</strong> días hábiles disponibles
          <span className="texto-secundario"> · {num(saldo.data.diasDevengados)} devengados · {saldo.data.diasTomados} tomados · {saldo.data.diasPendientesAprobacion} pendientes</span>
        </p>
      )}
      {historial.data && (historial.data.length === 0 ? <Vacio>Sin solicitudes registradas.</Vacio> : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead><tr><th>Período</th><th>Días hábiles</th><th>Estado</th><th>Comentario</th></tr></thead>
            <tbody>
              {historial.data.map((s) => (
                <tr key={s.id}>
                  <td className="mono">{fecha(s.fechaInicio)} → {fecha(s.fechaFin)}</td>
                  <td>{s.diasHabiles}</td>
                  <td><PillEstado estado={s.estado} /></td>
                  <td>{s.motivoRechazo ?? s.comentario ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </section>
  )
}

function Seguros({ empleado, puedeGestionar }: { empleado: EmpleadoDetalle; puedeGestionar: boolean }) {
  const queryClient = useQueryClient()
  const [afiliando, setAfiliando] = useState(false)
  const afiliaciones = useQuery({ queryKey: ['seguros', empleado.id], queryFn: () => segurosApi.afiliaciones(empleado.id) })

  const terminar = useMutation({
    mutationFn: (afiliacionId: number) => segurosApi.terminar(empleado.id, afiliacionId, hoyIso()),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['seguros', empleado.id] }),
  })

  return (
    <section className="seccion">
      <div className="titulo-con-accion">
        <h2>Seguros complementarios</h2>
        {puedeGestionar && empleado.activo && (
          <button className="boton boton-secundario" onClick={() => setAfiliando(true)}>Afiliar a un plan</button>
        )}
      </div>
      <Aviso>{terminar.error && mensajeError(terminar.error)}</Aviso>
      {afiliaciones.data && (afiliaciones.data.length === 0 ? <Vacio>Sin seguros.</Vacio> : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead><tr><th>Plan</th><th>Tipo</th><th>Prima mensual</th><th>Cargas</th><th>Vigencia</th><th /></tr></thead>
            <tbody>
              {afiliaciones.data.map((a) => (
                <tr key={a.id}>
                  <td>{a.plan}<br /><small className="texto-secundario">{a.aseguradora}</small></td>
                  <td>{ETIQUETA_TIPO_SEGURO[a.tipo]}</td>
                  <td className="mono">{formatoUf(a.primaMensualUf)}</td>
                  <td>{a.numeroCargas}</td>
                  <td>
                    <PillActivo activo={a.vigente} textoActivo="Vigente" textoInactivo="Terminado" />
                    <br /><small className="texto-secundario mono">{fecha(a.fechaInicio)} → {a.fechaTermino ? fecha(a.fechaTermino) : 'sin término'}</small>
                  </td>
                  <td>
                    {puedeGestionar && a.fechaTermino === null && (
                      <button className="boton boton-texto" disabled={terminar.isPending} onClick={() => terminar.mutate(a.id)}>
                        Terminar hoy
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
      <DialogoAfiliar empleadoId={empleado.id} abierto={afiliando} onCerrar={() => setAfiliando(false)} />
    </section>
  )
}

function DialogoAfiliar({ empleadoId, abierto, onCerrar }: { empleadoId: number; abierto: boolean; onCerrar: () => void }) {
  const queryClient = useQueryClient()
  const planes = useQuery({ queryKey: ['planes', true], queryFn: () => segurosApi.planes(true), enabled: abierto })
  const [planId, setPlanId] = useState('')
  const [inicio, setInicio] = useState(hoyIso())
  const [cargas, setCargas] = useState('0')

  const afiliar = useMutation({
    mutationFn: () => segurosApi.afiliar(empleadoId, Number(planId), inicio, Number(cargas)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['seguros', empleadoId] })
      onCerrar()
    },
  })

  return (
    <Dialogo abierto={abierto} titulo="Afiliar a un plan de seguro" onCerrar={onCerrar}>
      <form className="formulario" onSubmit={(ev) => { ev.preventDefault(); afiliar.mutate() }}>
        <Campo etiqueta="Plan" id="afiliar-plan">
          <select id="afiliar-plan" required value={planId} onChange={(ev) => setPlanId(ev.target.value)}>
            <option value="">Seleccione…</option>
            {planes.data?.map((p) => <option key={p.id} value={p.id}>{p.nombre} · {p.aseguradora} ({formatoUf(p.primaMensualUf)})</option>)}
          </select>
        </Campo>
        <Campo etiqueta="Fecha de inicio" id="afiliar-inicio">
          <input id="afiliar-inicio" type="date" required value={inicio} onChange={(ev) => setInicio(ev.target.value)} />
        </Campo>
        <Campo etiqueta="Número de cargas" id="afiliar-cargas">
          <input id="afiliar-cargas" type="number" min={0} max={15} required value={cargas} onChange={(ev) => setCargas(ev.target.value)} />
        </Campo>
        <Aviso>{afiliar.error && mensajeError(afiliar.error)}</Aviso>
        <div className="acciones">
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>Cancelar</button>
          <button className="boton boton-primario" disabled={afiliar.isPending}>Afiliar</button>
        </div>
      </form>
    </Dialogo>
  )
}

function DialogoDesvincular({ empleado, abierto, onCerrar }: { empleado: EmpleadoDetalle; abierto: boolean; onCerrar: () => void }) {
  const queryClient = useQueryClient()
  const [fechaTermino, setFechaTermino] = useState(hoyIso())

  const desvincular = useMutation({
    mutationFn: () => empleadosApi.desvincular(empleado.id, fechaTermino),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['empleado', empleado.id] })
      await queryClient.invalidateQueries({ queryKey: ['empleados'] })
      onCerrar()
    },
  })

  return (
    <Dialogo abierto={abierto} titulo={`Desvincular a ${empleado.nombres} ${empleado.apellidoPaterno}`} onCerrar={onCerrar}>
      <form className="formulario" onSubmit={(ev) => { ev.preventDefault(); desvincular.mutate() }}>
        <p>El empleado dejará de tener acceso al sistema. Si tiene equipo a cargo, primero reasigne su jefatura.</p>
        <Campo etiqueta="Fecha de término" id="desvincular-fecha">
          <input id="desvincular-fecha" type="date" required value={fechaTermino} min={empleado.fechaIngreso}
            onChange={(ev) => setFechaTermino(ev.target.value)} />
        </Campo>
        <Aviso>{desvincular.error && mensajeError(desvincular.error)}</Aviso>
        <div className="acciones">
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>Cancelar</button>
          <button className="boton boton-peligro" disabled={desvincular.isPending}>Confirmar desvinculación</button>
        </div>
      </form>
    </Dialogo>
  )
}
