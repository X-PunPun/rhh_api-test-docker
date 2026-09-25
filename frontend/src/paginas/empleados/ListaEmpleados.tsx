import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { empleadosApi } from '../../api/endpoints'
import type { FiltroEmpleados } from '../../api/tipos'
import { useAuth } from '../../auth/contexto'
import { Aviso, Cargando, EncabezadoPagina, Paginacion, PillActivo, Vacio } from '../../comun/componentes/Basicos'
import { useCargos, useComunas, useDepartamentos, useRegiones } from '../../comun/consultas'
import { fecha, mensajeError } from '../../comun/formato'

/** Los filtros viven en la URL: se puede recargar, volver atrás o compartir la búsqueda. */
function leerFiltro(params: URLSearchParams): FiltroEmpleados {
  const numero = (clave: string) => (params.get(clave) ? Number(params.get(clave)) : undefined)
  const activo = params.get('activo')
  return {
    regionId: numero('regionId'),
    comunaId: numero('comunaId'),
    departamentoId: numero('departamentoId'),
    cargoId: numero('cargoId'),
    activo: activo === null ? true : activo === 'todos' ? undefined : activo === 'true',
    busqueda: params.get('busqueda') ?? undefined,
    pagina: numero('pagina') ?? 1,
    tamanoPagina: 15,
  }
}

export function ListaEmpleados() {
  const { esGestor } = useAuth()
  const [params, setParams] = useSearchParams()
  const filtro = leerFiltro(params)
  const [texto, setTexto] = useState(filtro.busqueda ?? '')
  const [exportando, setExportando] = useState(false)
  const [errorExportar, setErrorExportar] = useState<string | null>(null)

  const regiones = useRegiones()
  const comunas = useComunas(filtro.regionId)
  const departamentos = useDepartamentos()
  const cargos = useCargos(filtro.departamentoId)

  const { data, isLoading, isFetching, error } = useQuery({
    queryKey: ['empleados', filtro],
    queryFn: () => empleadosApi.buscar(filtro),
    placeholderData: keepPreviousData,
  })

  const cambiar = (cambios: Record<string, string | undefined>) => {
    const siguiente = new URLSearchParams(params)
    for (const [clave, valor] of Object.entries(cambios)) {
      if (valor === undefined || valor === '') siguiente.delete(clave)
      else siguiente.set(clave, valor)
    }
    if (!('pagina' in cambios)) siguiente.delete('pagina')
    setParams(siguiente)
  }

  const exportar = async () => {
    setExportando(true)
    setErrorExportar(null)
    try {
      await empleadosApi.exportarExcel({ ...filtro, pagina: undefined, tamanoPagina: undefined })
    } catch (e) {
      setErrorExportar(mensajeError(e))
    } finally {
      setExportando(false)
    }
  }

  return (
    <>
      <EncabezadoPagina
        titulo={esGestor ? 'Empleados' : 'Mi equipo'}
        descripcion="Solo se muestran las personas dentro de su alcance."
        acciones={esGestor && (
          <>
            <button className="boton boton-secundario" onClick={exportar} disabled={exportando}>
              {exportando ? 'Generando…' : 'Exportar a Excel'}
            </button>
            <Link className="boton boton-primario" to="/empleados/nuevo">Nuevo empleado</Link>
          </>
        )}
      />

      <form className="filtros" onSubmit={(e) => { e.preventDefault(); cambiar({ busqueda: texto.trim() }) }} role="search">
        <input
          id="filtro-busqueda"
          type="search"
          placeholder="Nombre, email o RUT"
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          aria-label="Buscar por nombre, email o RUT"
        />
        <select id="filtro-region" aria-label="Región" value={filtro.regionId ?? ''}
          onChange={(e) => cambiar({ regionId: e.target.value, comunaId: undefined })}>
          <option value="">Todas las regiones</option>
          {regiones.data?.map((r) => <option key={r.id} value={r.id}>{r.abreviatura} · {r.nombre}</option>)}
        </select>
        <select id="filtro-comuna" aria-label="Comuna" value={filtro.comunaId ?? ''} disabled={!filtro.regionId}
          onChange={(e) => cambiar({ comunaId: e.target.value })}>
          <option value="">Todas las comunas</option>
          {comunas.data?.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
        </select>
        <select id="filtro-departamento" aria-label="Departamento" value={filtro.departamentoId ?? ''}
          onChange={(e) => cambiar({ departamentoId: e.target.value, cargoId: undefined })}>
          <option value="">Todos los departamentos</option>
          {departamentos.data?.map((d) => <option key={d.id} value={d.id}>{d.nombre}</option>)}
        </select>
        <select id="filtro-cargo" aria-label="Cargo" value={filtro.cargoId ?? ''} disabled={!filtro.departamentoId}
          onChange={(e) => cambiar({ cargoId: e.target.value })}>
          <option value="">Todos los cargos</option>
          {cargos.data?.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
        </select>
        <select id="filtro-estado" aria-label="Estado" value={params.get('activo') ?? 'true'}
          onChange={(e) => cambiar({ activo: e.target.value === 'true' ? undefined : e.target.value })}>
          <option value="true">Activos</option>
          <option value="false">Desvinculados</option>
          <option value="todos">Todos</option>
        </select>
        <button className="boton boton-secundario" type="submit">Buscar</button>
      </form>

      <Aviso>{errorExportar}</Aviso>
      {isLoading && <Cargando />}
      {error && <Aviso>{mensajeError(error)}</Aviso>}

      {data && (data.items.length === 0 ? (
        <Vacio>No hay empleados que coincidan con los filtros.</Vacio>
      ) : (
        <div className={`tabla-contenedor${isFetching ? ' actualizando' : ''}`}>
          <table className="tabla">
            <thead>
              <tr>
                <th>Nombre</th><th>RUT</th><th>Cargo</th><th>Departamento</th>
                <th>Región / comuna</th><th>Jefatura</th><th>Ingreso</th><th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((e) => (
                <tr key={e.id}>
                  <td><Link to={`/empleados/${e.id}`}>{e.nombreCompleto}</Link><br /><small className="texto-secundario">{e.email}</small></td>
                  <td className="mono">{e.rut}</td>
                  <td>{e.cargo}</td>
                  <td>{e.departamento}</td>
                  <td>{e.region}<br /><small className="texto-secundario">{e.comuna}</small></td>
                  <td>{e.jefe ?? '—'}</td>
                  <td className="mono">{fecha(e.fechaIngreso)}</td>
                  <td><PillActivo activo={e.activo} textoInactivo="Desvinculado" /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}

      {data && (
        <Paginacion pagina={data.numeroPagina} totalPaginas={data.totalPaginas} total={data.total}
          onCambiar={(p) => cambiar({ pagina: String(p) })} />
      )}
    </>
  )
}
