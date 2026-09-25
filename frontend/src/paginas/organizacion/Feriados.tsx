import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { vacacionesApi } from '../../api/endpoints'
import { Aviso, Campo, Cargando, EncabezadoPagina, Vacio } from '../../comun/componentes/Basicos'
import { fecha, mensajeError } from '../../comun/formato'

const DIAS = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado']

export function Feriados() {
  const queryClient = useQueryClient()
  const [anio, setAnio] = useState(new Date().getFullYear())
  const [nuevaFecha, setNuevaFecha] = useState('')
  const [nombre, setNombre] = useState('')

  const feriados = useQuery({ queryKey: ['feriados', anio], queryFn: () => vacacionesApi.feriados(anio) })
  const refrescar = () => queryClient.invalidateQueries({ queryKey: ['feriados'] })

  const crear = useMutation({
    mutationFn: () => vacacionesApi.crearFeriado(nuevaFecha, nombre.trim()),
    onSuccess: async () => { setNuevaFecha(''); setNombre(''); await refrescar() },
  })
  const eliminar = useMutation({ mutationFn: vacacionesApi.eliminarFeriado, onSuccess: refrescar })

  return (
    <>
      <EncabezadoPagina titulo="Feriados" descripcion="No se descuentan de las vacaciones. Revise cada año el calendario oficial." />
      <form className="formulario-en-linea tarjeta" onSubmit={(e) => { e.preventDefault(); crear.mutate() }}>
        <Campo etiqueta="Fecha" id="feriado-fecha">
          <input id="feriado-fecha" type="date" required value={nuevaFecha} onChange={(e) => setNuevaFecha(e.target.value)} />
        </Campo>
        <Campo etiqueta="Nombre" id="feriado-nombre">
          <input id="feriado-nombre" required maxLength={150} value={nombre} onChange={(e) => setNombre(e.target.value)} />
        </Campo>
        <button className="boton boton-primario" disabled={crear.isPending}>Agregar feriado</button>
      </form>
      <Aviso>{(crear.error ?? eliminar.error) && mensajeError(crear.error ?? eliminar.error)}</Aviso>

      <div className="titulo-con-accion">
        <h2>Año {anio}</h2>
        <div className="acciones">
          <button className="boton boton-secundario" onClick={() => setAnio(anio - 1)}>← {anio - 1}</button>
          <button className="boton boton-secundario" onClick={() => setAnio(anio + 1)}>{anio + 1} →</button>
        </div>
      </div>
      {feriados.isLoading && <Cargando />}
      {feriados.data && (feriados.data.length === 0 ? <Vacio>No hay feriados cargados para {anio}.</Vacio> : (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead><tr><th>Fecha</th><th>Día</th><th>Nombre</th><th /></tr></thead>
            <tbody>
              {feriados.data.map((f) => (
                <tr key={f.id}>
                  <td className="mono">{fecha(f.fecha)}</td>
                  <td>{DIAS[new Date(`${f.fecha}T00:00:00Z`).getUTCDay()]}</td>
                  <td>{f.nombre}</td>
                  <td>
                    <button className="boton boton-texto" disabled={eliminar.isPending} onClick={() => eliminar.mutate(f.id)}>Eliminar</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </>
  )
}
