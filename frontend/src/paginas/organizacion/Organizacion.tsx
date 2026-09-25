import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { organizacionApi } from '../../api/endpoints'
import type { Departamento } from '../../api/tipos'
import { Aviso, Campo, Cargando, EncabezadoPagina, PillActivo } from '../../comun/componentes/Basicos'
import { useCargos, useDepartamentos } from '../../comun/consultas'
import { mensajeError } from '../../comun/formato'

export function Organizacion() {
  const departamentos = useDepartamentos()
  const [seleccionado, setSeleccionado] = useState<Departamento | null>(null)

  return (
    <>
      <EncabezadoPagina titulo="Departamentos y cargos" descripcion="Seleccione un departamento para ver y crear sus cargos." />
      <div className="grilla-2">
        <section className="tarjeta">
          <h2>Departamentos</h2>
          <NuevoDepartamento />
          {departamentos.isLoading && <Cargando />}
          <ul className="lista-seleccion">
            {departamentos.data?.map((d) => (
              <li key={d.id}>
                <button className={seleccionado?.id === d.id ? 'seleccionado' : undefined} onClick={() => setSeleccionado(d)}>
                  <span>
                    <strong>{d.nombre}</strong>
                    <small className="texto-secundario"> · {d.empleadosActivos} activo(s)</small>
                  </span>
                  <PillActivo activo={d.activo} />
                </button>
              </li>
            ))}
          </ul>
        </section>
        <section className="tarjeta">
          {seleccionado ? <Cargos departamento={seleccionado} onCambio={setSeleccionado} /> : (
            <p className="texto-secundario">Ningún departamento seleccionado.</p>
          )}
        </section>
      </div>
    </>
  )
}

function NuevoDepartamento() {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState('')
  const crear = useMutation({
    mutationFn: () => organizacionApi.crearDepartamento(nombre.trim(), null),
    onSuccess: async () => {
      setNombre('')
      await queryClient.invalidateQueries({ queryKey: ['departamentos'] })
    },
  })

  return (
    <form className="formulario-en-linea" onSubmit={(e) => { e.preventDefault(); crear.mutate() }}>
      <input id="nuevo-departamento" aria-label="Nombre del nuevo departamento" placeholder="Nuevo departamento"
        maxLength={100} required value={nombre} onChange={(e) => setNombre(e.target.value)} />
      <button className="boton boton-primario" disabled={crear.isPending}>Agregar</button>
      <Aviso>{crear.error && mensajeError(crear.error)}</Aviso>
    </form>
  )
}

function Cargos({ departamento, onCambio }: { departamento: Departamento; onCambio: (d: Departamento | null) => void }) {
  const queryClient = useQueryClient()
  const cargos = useCargos(departamento.id)
  const [nombre, setNombre] = useState('')

  const refrescar = () => Promise.all([
    queryClient.invalidateQueries({ queryKey: ['cargos'] }),
    queryClient.invalidateQueries({ queryKey: ['departamentos'] }),
  ])

  const crear = useMutation({
    mutationFn: () => organizacionApi.crearCargo(nombre.trim(), departamento.id),
    onSuccess: async () => { setNombre(''); await refrescar() },
  })
  const desactivarCargo = useMutation({ mutationFn: organizacionApi.desactivarCargo, onSuccess: refrescar })
  const estado = useMutation({
    mutationFn: () => organizacionApi.estadoDepartamento(departamento.id, !departamento.activo),
    onSuccess: async () => {
      await refrescar()
      onCambio({ ...departamento, activo: !departamento.activo })
    },
  })

  const error = crear.error ?? desactivarCargo.error ?? estado.error

  return (
    <>
      <div className="titulo-con-accion">
        <h2>{departamento.nombre}</h2>
        <button className="boton boton-secundario" disabled={estado.isPending} onClick={() => estado.mutate()}>
          {departamento.activo ? 'Desactivar departamento' : 'Activar departamento'}
        </button>
      </div>
      <Aviso>{error && mensajeError(error)}</Aviso>
      {departamento.activo && (
        <form className="formulario-en-linea" onSubmit={(e) => { e.preventDefault(); crear.mutate() }}>
          <Campo etiqueta="Nuevo cargo" id="nuevo-cargo">
            <input id="nuevo-cargo" maxLength={100} required value={nombre} onChange={(e) => setNombre(e.target.value)} />
          </Campo>
          <button className="boton boton-primario" disabled={crear.isPending}>Agregar cargo</button>
        </form>
      )}
      {cargos.isLoading && <Cargando />}
      <ul className="lista-simple">
        {cargos.data?.map((c) => (
          <li key={c.id} className="fila-con-accion">
            <span>{c.nombre} <PillActivo activo={c.activo} /></span>
            {c.activo && (
              <button className="boton boton-texto" disabled={desactivarCargo.isPending} onClick={() => desactivarCargo.mutate(c.id)}>
                Desactivar
              </button>
            )}
          </li>
        ))}
      </ul>
    </>
  )
}
