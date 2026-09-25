import { Suspense, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import type { Rol } from '../api/tipos'
import { useAuth } from '../auth/contexto'

interface Enlace { a: string; texto: string; roles?: Rol[] }
interface Grupo { titulo: string; enlaces: Enlace[] }

const GESTOR: Rol[] = ['Admin', 'RRHH']

/** El menú se arma según el rol: cada usuario ve solo lo que puede usar. */
const MENU: Grupo[] = [
  {
    titulo: 'Personal',
    enlaces: [
      { a: '/', texto: 'Inicio' },
      { a: '/empleados', texto: 'Empleados', roles: GESTOR },
      { a: '/empleados', texto: 'Mi equipo', roles: ['Jefatura'] },
    ],
  },
  {
    titulo: 'Vacaciones',
    enlaces: [
      { a: '/vacaciones', texto: 'Mis vacaciones' },
      { a: '/vacaciones/aprobaciones', texto: 'Aprobaciones', roles: ['Admin', 'RRHH', 'Jefatura'] },
      { a: '/feriados', texto: 'Feriados', roles: GESTOR },
    ],
  },
  {
    titulo: 'Configuración',
    enlaces: [
      { a: '/organizacion', texto: 'Departamentos y cargos', roles: GESTOR },
      { a: '/seguros', texto: 'Planes de seguro', roles: GESTOR },
      { a: '/usuarios', texto: 'Usuarios', roles: ['Admin'] },
      { a: '/auditoria', texto: 'Auditoría', roles: ['Admin'] },
    ],
  },
]

export function Layout() {
  const { usuario, cerrarSesion } = useAuth()
  const navigate = useNavigate()
  const [menuAbierto, setMenuAbierto] = useState(false)

  if (!usuario) return null

  const grupos = MENU
    .map((g) => ({ ...g, enlaces: g.enlaces.filter((e) => !e.roles || e.roles.includes(usuario.rol)) }))
    .filter((g) => g.enlaces.length > 0)

  const salir = async () => {
    await cerrarSesion()
    navigate('/login', { replace: true })
  }

  return (
    <div className={`app${menuAbierto ? ' menu-abierto' : ''}`}>
      <aside className="barra-lateral">
        <div className="marca">
          <span className="marca-logo" aria-hidden="true">RH</span>
          <span>Gestión de personas</span>
        </div>
        <nav aria-label="Menú principal" onClick={() => setMenuAbierto(false)}>
          {grupos.map((g) => (
            <div key={g.titulo} className="menu-grupo">
              <span className="menu-titulo">{g.titulo}</span>
              {g.enlaces.map((e) => (
                <NavLink key={e.texto} to={e.a} end={e.a === '/' || e.a === '/vacaciones'}>{e.texto}</NavLink>
              ))}
            </div>
          ))}
        </nav>
      </aside>

      <div className="contenido">
        <header className="barra-superior">
          <button className="boton-icono solo-movil" onClick={() => setMenuAbierto((v) => !v)} aria-label="Abrir menú">☰</button>
          <div className="usuario-actual">
            <span>
              <strong>{usuario.nombre ?? usuario.email}</strong>
              <span className="texto-secundario"> · {usuario.rol}</span>
            </span>
            {usuario.empleadoId && <NavLink to={`/empleados/${usuario.empleadoId}`}>Mi ficha</NavLink>}
            <NavLink to="/cuenta">Mi cuenta</NavLink>
            <button className="boton boton-secundario" onClick={salir}>Cerrar sesión</button>
          </div>
        </header>
        <main className="pagina">
          <Suspense fallback={<p className="cargando">Cargando…</p>}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  )
}
