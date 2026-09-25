import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { segurosApi } from '../../api/endpoints'
import { ETIQUETA_TIPO_SEGURO, TIPOS_SEGURO, type TipoSeguro } from '../../api/tipos'
import { Aviso, Campo, Cargando, EncabezadoPagina, PillActivo } from '../../comun/componentes/Basicos'
import { formatoUf, mensajeError } from '../../comun/formato'

export function PlanesSeguro() {
  const queryClient = useQueryClient()
  const planes = useQuery({ queryKey: ['planes'], queryFn: () => segurosApi.planes() })
  const [nombre, setNombre] = useState('')
  const [aseguradora, setAseguradora] = useState('')
  const [tipo, setTipo] = useState<TipoSeguro>('Salud')
  const [prima, setPrima] = useState('')

  const refrescar = () => queryClient.invalidateQueries({ queryKey: ['planes'] })
  const crear = useMutation({
    mutationFn: () => segurosApi.crearPlan({ nombre: nombre.trim(), aseguradora: aseguradora.trim(), tipo, primaMensualUf: Number(prima) }),
    onSuccess: async () => { setNombre(''); setAseguradora(''); setPrima(''); await refrescar() },
  })
  const estado = useMutation({
    mutationFn: ({ id, activo }: { id: number; activo: boolean }) => segurosApi.estadoPlan(id, activo),
    onSuccess: refrescar,
  })

  return (
    <>
      <EncabezadoPagina titulo="Planes de seguro" descripcion="Seguros complementarios disponibles para afiliar al personal. La prima se expresa en UF por titular." />
      <form className="formulario tarjeta" onSubmit={(e) => { e.preventDefault(); crear.mutate() }}>
        <div className="grilla-campos">
          <Campo etiqueta="Nombre del plan" id="plan-nombre">
            <input id="plan-nombre" required maxLength={100} value={nombre} onChange={(e) => setNombre(e.target.value)} />
          </Campo>
          <Campo etiqueta="Aseguradora" id="plan-aseguradora">
            <input id="plan-aseguradora" required maxLength={100} value={aseguradora} onChange={(e) => setAseguradora(e.target.value)} />
          </Campo>
          <Campo etiqueta="Tipo" id="plan-tipo">
            <select id="plan-tipo" value={tipo} onChange={(e) => setTipo(e.target.value as TipoSeguro)}>
              {TIPOS_SEGURO.map((t) => <option key={t} value={t}>{ETIQUETA_TIPO_SEGURO[t]}</option>)}
            </select>
          </Campo>
          <Campo etiqueta="Prima mensual (UF)" id="plan-prima">
            <input id="plan-prima" type="number" required min={0} max={1000} step="0.0001" value={prima} onChange={(e) => setPrima(e.target.value)} />
          </Campo>
        </div>
        <Aviso>{(crear.error ?? estado.error) && mensajeError(crear.error ?? estado.error)}</Aviso>
        <div className="acciones"><button className="boton boton-primario" disabled={crear.isPending}>Crear plan</button></div>
      </form>

      {planes.isLoading && <Cargando />}
      {planes.data && (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead><tr><th>Plan</th><th>Aseguradora</th><th>Tipo</th><th>Prima mensual</th><th>Estado</th><th /></tr></thead>
            <tbody>
              {planes.data.map((p) => (
                <tr key={p.id}>
                  <td>{p.nombre}</td>
                  <td>{p.aseguradora}</td>
                  <td>{ETIQUETA_TIPO_SEGURO[p.tipo]}</td>
                  <td className="mono">{formatoUf(p.primaMensualUf)}</td>
                  <td><PillActivo activo={p.activo} /></td>
                  <td>
                    <button className="boton boton-texto" disabled={estado.isPending} onClick={() => estado.mutate({ id: p.id, activo: !p.activo })}>
                      {p.activo ? 'Desactivar' : 'Activar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
