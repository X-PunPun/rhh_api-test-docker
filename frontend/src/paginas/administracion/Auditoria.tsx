import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { usuariosApi } from '../../api/endpoints'
import { Aviso, Cargando, EncabezadoPagina, Paginacion, Vacio } from '../../comun/componentes/Basicos'
import { fechaYHora, mensajeError } from '../../comun/formato'

const FILTROS = [
  ['', 'Todas las acciones'],
  ['login', 'Inicios de sesión'],
  ['POST', 'Creaciones y acciones (POST)'],
  ['PUT', 'Modificaciones (PUT)'],
  ['DELETE', 'Eliminaciones'],
  ['usuario.', 'Administración de usuarios'],
  ['token.', 'Tokens reutilizados'],
] as const

function claseCodigo(codigo: number | null): string {
  if (codigo === null) return 'pill-neutro'
  if (codigo >= 500 || codigo === 401 || codigo === 403) return 'pill-critico'
  if (codigo >= 400) return 'pill-advertencia'
  return 'pill-ok'
}

export function Auditoria() {
  const [accion, setAccion] = useState('')
  const [pagina, setPagina] = useState(1)
  const { data, isLoading, error } = useQuery({
    queryKey: ['auditoria', accion, pagina],
    queryFn: () => usuariosApi.auditoria({ accion: accion || undefined, pagina, tamanoPagina: 25 }),
    placeholderData: keepPreviousData,
  })

  return (
    <>
      <EncabezadoPagina titulo="Auditoría" descripcion="Quién hizo qué y cuándo: operaciones que modifican datos e intentos de inicio de sesión." />
      <div className="filtros">
        <select id="filtro-accion" aria-label="Tipo de acción" value={accion} onChange={(e) => { setAccion(e.target.value); setPagina(1) }}>
          {FILTROS.map(([valor, texto]) => <option key={valor} value={valor}>{texto}</option>)}
        </select>
      </div>
      {isLoading && <Cargando />}
      {error && <Aviso>{mensajeError(error)}</Aviso>}
      {data && (data.items.length === 0 ? <Vacio>Sin registros.</Vacio> : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead><tr><th>Fecha</th><th>Usuario</th><th>Acción</th><th>Detalle</th><th>Resultado</th><th>IP</th></tr></thead>
            <tbody>
              {data.items.map((r) => (
                <tr key={r.id}>
                  <td className="mono">{fechaYHora(r.fecha)}</td>
                  <td>{r.email ?? '—'}</td>
                  <td className="mono">{r.accion}</td>
                  <td>{r.detalle ?? '—'}</td>
                  <td><span className={`pill ${claseCodigo(r.codigoResultado)}`}>{r.codigoResultado ?? '—'}</span></td>
                  <td className="mono">{r.ip ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
      {data && <Paginacion pagina={data.numeroPagina} totalPaginas={data.totalPaginas} total={data.total} onCambiar={setPagina} />}
    </>
  )
}
