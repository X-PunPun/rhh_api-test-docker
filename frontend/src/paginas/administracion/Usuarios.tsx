import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { empleadosApi, usuariosApi } from '../../api/endpoints'
import { ROLES, type Rol, type Usuario } from '../../api/tipos'
import { useAuth } from '../../auth/contexto'
import { Aviso, Campo, Cargando, Dialogo, EncabezadoPagina, PillActivo } from '../../comun/componentes/Basicos'
import { useRegiones } from '../../comun/consultas'
import { fechaYHora, mensajeError } from '../../comun/formato'

export function Usuarios() {
  const { usuario: yo } = useAuth()
  const queryClient = useQueryClient()
  const usuarios = useQuery({ queryKey: ['usuarios'], queryFn: usuariosApi.listar })
  const [creando, setCreando] = useState(false)
  const [editandoRol, setEditandoRol] = useState<Usuario | null>(null)
  const [restableciendo, setRestableciendo] = useState<Usuario | null>(null)

  const refrescar = () => queryClient.invalidateQueries({ queryKey: ['usuarios'] })
  const estado = useMutation({ mutationFn: ({ id, activo }: { id: number; activo: boolean }) => usuariosApi.estado(id, activo), onSuccess: refrescar })
  const desbloquear = useMutation({ mutationFn: usuariosApi.desbloquear, onSuccess: refrescar })
  const error = estado.error ?? desbloquear.error

  return (
    <>
      <EncabezadoPagina
        titulo="Usuarios"
        descripcion="Cuentas de acceso, roles y regiones. Cambiar el rol o desactivar cierra las sesiones del usuario."
        acciones={<button className="boton boton-primario" onClick={() => setCreando(true)}>Nuevo usuario</button>}
      />
      <Aviso>{error && mensajeError(error)}</Aviso>
      {usuarios.isLoading && <Cargando />}
      {usuarios.data && (
        <div className="tabla-contenedor">
          <table className="tabla">
            <thead><tr><th>Email</th><th>Empleado</th><th>Rol</th><th>Regiones</th><th>Estado</th><th>Último acceso</th><th /></tr></thead>
            <tbody>
              {usuarios.data.map((u) => (
                <tr key={u.id}>
                  <td>{u.email}</td>
                  <td>{u.empleado ?? '—'}</td>
                  <td>{u.rol}</td>
                  <td className="mono">{u.regiones.length === 16 ? 'Todas' : u.regiones.join(', ') || '—'}</td>
                  <td>
                    <PillActivo activo={u.activo} />
                    {u.bloqueado && <> <span className="pill pill-critico">Bloqueado</span></>}
                  </td>
                  <td className="mono">{fechaYHora(u.ultimoAcceso)}</td>
                  <td className="acciones-fila">
                    {u.id !== yo?.usuarioId && (
                      <>
                        <button className="boton boton-texto" onClick={() => setEditandoRol(u)}>Rol</button>
                        <button className="boton boton-texto" onClick={() => estado.mutate({ id: u.id, activo: !u.activo })}>
                          {u.activo ? 'Desactivar' : 'Activar'}
                        </button>
                      </>
                    )}
                    {u.bloqueado && <button className="boton boton-texto" onClick={() => desbloquear.mutate(u.id)}>Desbloquear</button>}
                    <button className="boton boton-texto" onClick={() => setRestableciendo(u)}>Clave</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <DialogoCrear abierto={creando} onCerrar={() => setCreando(false)} onCreado={refrescar} />
      {editandoRol && <DialogoRol usuario={editandoRol} onCerrar={() => setEditandoRol(null)} onGuardado={refrescar} />}
      {restableciendo && <DialogoClave usuario={restableciendo} onCerrar={() => setRestableciendo(null)} />}
    </>
  )
}

function SelectorRegiones({ seleccion, onCambiar }: { seleccion: number[]; onCambiar: (r: number[]) => void }) {
  const regiones = useRegiones()
  const alternar = (id: number) => onCambiar(seleccion.includes(id) ? seleccion.filter((r) => r !== id) : [...seleccion, id])

  return (
    <fieldset className="selector-regiones">
      <legend>Regiones que gestiona</legend>
      <button type="button" className="boton boton-texto" onClick={() => onCambiar(regiones.data?.map((r) => r.id) ?? [])}>Todas</button>
      <div className="casillas">
        {regiones.data?.map((r) => (
          <label key={r.id}>
            <input type="checkbox" checked={seleccion.includes(r.id)} onChange={() => alternar(r.id)} /> {r.abreviatura} {r.nombre}
          </label>
        ))}
      </div>
    </fieldset>
  )
}

function DialogoCrear({ abierto, onCerrar, onCreado }: { abierto: boolean; onCerrar: () => void; onCreado: () => Promise<unknown> }) {
  const [empleadoId, setEmpleadoId] = useState('')
  const [email, setEmail] = useState('')
  const [rol, setRol] = useState<Rol>('Empleado')
  const [regiones, setRegiones] = useState<number[]>([])
  const [clave, setClave] = useState('')
  const empleados = useQuery({
    queryKey: ['empleados', 'para-usuarios'],
    queryFn: () => empleadosApi.buscar({ activo: true, tamanoPagina: 100 }),
    enabled: abierto,
  })

  const crear = useMutation({
    mutationFn: () => usuariosApi.crear({
      empleadoId: empleadoId ? Number(empleadoId) : null,
      email: empleadoId ? null : email.trim(),
      rol,
      regiones: rol === 'RRHH' ? regiones : [],
      claveInicial: clave,
    }),
    onSuccess: async () => {
      setEmpleadoId(''); setEmail(''); setRol('Empleado'); setRegiones([]); setClave('')
      await onCreado()
      onCerrar()
    },
  })

  return (
    <Dialogo abierto={abierto} titulo="Nuevo usuario" onCerrar={onCerrar}>
      <form className="formulario" onSubmit={(e) => { e.preventDefault(); crear.mutate() }}>
        <Campo etiqueta="Empleado" id="usuario-empleado" ayuda="El email se toma de la ficha del empleado.">
          <select id="usuario-empleado" value={empleadoId} onChange={(e) => setEmpleadoId(e.target.value)}>
            <option value="">Sin empleado (cuenta técnica)</option>
            {empleados.data?.items.map((e) => <option key={e.id} value={e.id}>{e.nombreCompleto} · {e.email}</option>)}
          </select>
        </Campo>
        {!empleadoId && (
          <Campo etiqueta="Email" id="usuario-email">
            <input id="usuario-email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </Campo>
        )}
        <Campo etiqueta="Rol" id="usuario-rol">
          <select id="usuario-rol" value={rol} onChange={(e) => setRol(e.target.value as Rol)}>
            {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
          </select>
        </Campo>
        {rol === 'RRHH' && <SelectorRegiones seleccion={regiones} onCambiar={setRegiones} />}
        <Campo etiqueta="Clave inicial" id="usuario-clave" ayuda="Mín. 10 caracteres con mayúscula, minúscula, número y símbolo.">
          <input id="usuario-clave" type="password" required minLength={10} autoComplete="new-password" value={clave} onChange={(e) => setClave(e.target.value)} />
        </Campo>
        <Aviso>{crear.error && mensajeError(crear.error)}</Aviso>
        <div className="acciones">
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>Cancelar</button>
          <button className="boton boton-primario" disabled={crear.isPending}>Crear usuario</button>
        </div>
      </form>
    </Dialogo>
  )
}

function DialogoRol({ usuario, onCerrar, onGuardado }: { usuario: Usuario; onCerrar: () => void; onGuardado: () => Promise<unknown> }) {
  const [rol, setRol] = useState<Rol>(usuario.rol)
  const [regiones, setRegiones] = useState<number[]>(usuario.regiones)
  const guardar = useMutation({
    mutationFn: () => usuariosApi.asignarRol(usuario.id, rol, rol === 'RRHH' ? regiones : []),
    onSuccess: async () => { await onGuardado(); onCerrar() },
  })

  return (
    <Dialogo abierto titulo={`Rol de ${usuario.email}`} onCerrar={onCerrar}>
      <form className="formulario" onSubmit={(e) => { e.preventDefault(); guardar.mutate() }}>
        <Campo etiqueta="Rol" id="rol-editar">
          <select id="rol-editar" value={rol} onChange={(e) => setRol(e.target.value as Rol)}>
            {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
          </select>
        </Campo>
        {rol === 'RRHH' && <SelectorRegiones seleccion={regiones} onCambiar={setRegiones} />}
        <Aviso>{guardar.error && mensajeError(guardar.error)}</Aviso>
        <div className="acciones">
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>Cancelar</button>
          <button className="boton boton-primario" disabled={guardar.isPending}>Guardar</button>
        </div>
      </form>
    </Dialogo>
  )
}

function DialogoClave({ usuario, onCerrar }: { usuario: Usuario; onCerrar: () => void }) {
  const [clave, setClave] = useState('')
  const restablecer = useMutation({ mutationFn: () => usuariosApi.restablecerClave(usuario.id, clave), onSuccess: onCerrar })

  return (
    <Dialogo abierto titulo={`Restablecer clave de ${usuario.email}`} onCerrar={onCerrar}>
      <form className="formulario" onSubmit={(e) => { e.preventDefault(); restablecer.mutate() }}>
        <p className="texto-secundario">Comunique la clave al usuario por un canal seguro. Sus sesiones abiertas se cerrarán.</p>
        <Campo etiqueta="Clave nueva" id="restablecer-clave">
          <input id="restablecer-clave" type="password" required minLength={10} autoComplete="new-password" value={clave} onChange={(e) => setClave(e.target.value)} />
        </Campo>
        <Aviso>{restablecer.error && mensajeError(restablecer.error)}</Aviso>
        <div className="acciones">
          <button type="button" className="boton boton-secundario" onClick={onCerrar}>Cancelar</button>
          <button className="boton boton-primario" disabled={restablecer.isPending}>Restablecer</button>
        </div>
      </form>
    </Dialogo>
  )
}
