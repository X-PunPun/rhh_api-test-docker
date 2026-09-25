import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { reportesApi, vacacionesApi } from '../api/endpoints'
import { useAuth } from '../auth/contexto'
import { BarrasHorizontales } from '../comun/componentes/BarrasHorizontales'
import { Aviso, Cargando, EncabezadoPagina, Indicador } from '../comun/componentes/Basicos'
import { fecha, mensajeError, num } from '../comun/formato'

export function Inicio() {
  const { usuario, esGestor } = useAuth()

  return (
    <>
      <EncabezadoPagina
        titulo={`Hola, ${usuario?.nombre?.split(' ')[0] ?? usuario?.email}`}
        descripcion={esGestor ? 'Resumen de la dotación dentro de su alcance.' : 'Su resumen de vacaciones.'}
      />
      {esGestor && <ResumenGestor />}
      {usuario?.empleadoId && <ResumenPersonal empleadoId={usuario.empleadoId} esJefatura={usuario.rol === 'Jefatura'} />}
    </>
  )
}

function ResumenGestor() {
  const { data, isLoading, error } = useQuery({ queryKey: ['resumen'], queryFn: reportesApi.resumen })

  if (isLoading) return <Cargando />
  if (error) return <Aviso>{mensajeError(error)}</Aviso>
  if (!data) return null

  return (
    <section className="seccion">
      <div className="indicadores">
        <Indicador etiqueta="Empleados activos" valor={num(data.empleadosActivos)} />
        <Indicador etiqueta="Desvinculados" valor={num(data.empleadosDesvinculados)} />
        <Indicador etiqueta="Vacaciones por aprobar" valor={num(data.solicitudesVacacionesPendientes)} />
        <Indicador etiqueta="Seguros vigentes" valor={num(data.afiliacionesSeguroVigentes)} />
      </div>
      <div className="grilla-2">
        <BarrasHorizontales titulo="Dotación por región" datos={data.empleadosPorRegion} />
        <BarrasHorizontales titulo="Dotación por departamento" datos={data.empleadosPorDepartamento} />
      </div>
      <p className="texto-secundario">Datos al {fecha(data.fecha)}.</p>
    </section>
  )
}

function ResumenPersonal({ empleadoId, esJefatura }: { empleadoId: number; esJefatura: boolean }) {
  const saldo = useQuery({ queryKey: ['saldo', empleadoId], queryFn: () => vacacionesApi.saldo(empleadoId) })
  const pendientes = useQuery({
    queryKey: ['pendientes-equipo'],
    queryFn: vacacionesApi.pendientesEquipo,
    enabled: esJefatura,
  })

  return (
    <section className="seccion">
      <h2>Mis vacaciones</h2>
      {saldo.isLoading && <Cargando />}
      {saldo.error && <Aviso>{mensajeError(saldo.error)}</Aviso>}
      {saldo.data && (
        <div className="indicadores">
          <Indicador etiqueta="Días disponibles" valor={num(saldo.data.diasDisponibles)} detalle="días hábiles" />
          <Indicador etiqueta="Tomados" valor={num(saldo.data.diasTomados)} />
          <Indicador etiqueta="Pendientes de aprobación" valor={num(saldo.data.diasPendientesAprobacion)} />
          <Indicador etiqueta="Años de servicio" valor={saldo.data.aniosServicio}
            detalle={saldo.data.diasProgresivosAnuales > 0 ? `+${saldo.data.diasProgresivosAnuales} día(s) progresivo(s)` : undefined} />
        </div>
      )}
      <div className="acciones">
        <Link className="boton boton-primario" to="/vacaciones">Solicitar vacaciones</Link>
        {esJefatura && (
          <Link className="boton boton-secundario" to="/vacaciones/aprobaciones">
            Aprobaciones pendientes{pendientes.data ? ` (${pendientes.data.length})` : ''}
          </Link>
        )}
      </div>
    </section>
  )
}
