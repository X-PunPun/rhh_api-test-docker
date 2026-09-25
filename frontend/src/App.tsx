import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createBrowserRouter, Link, RouterProvider } from 'react-router'
import { ErrorApi } from './api/cliente'
import { AuthProvider } from './auth/AuthContext'
import { RutaProtegida } from './auth/RutaProtegida'
import { Layout } from './layout/Layout'
import { Inicio } from './paginas/Inicio'
import { Login } from './paginas/Login'
import type { Rol } from './api/tipos'
import { lazy, type ReactNode } from 'react'

// Cada pantalla se descarga recién cuando se visita (code splitting): el login carga liviano.
const Auditoria = lazy(() => import('./paginas/administracion/Auditoria').then((m) => ({ default: m.Auditoria })))
const Usuarios = lazy(() => import('./paginas/administracion/Usuarios').then((m) => ({ default: m.Usuarios })))
const MiCuenta = lazy(() => import('./paginas/cuenta/MiCuenta').then((m) => ({ default: m.MiCuenta })))
const DetalleEmpleado = lazy(() => import('./paginas/empleados/DetalleEmpleado').then((m) => ({ default: m.DetalleEmpleado })))
const FormularioEmpleado = lazy(() => import('./paginas/empleados/FormularioEmpleado').then((m) => ({ default: m.FormularioEmpleado })))
const ListaEmpleados = lazy(() => import('./paginas/empleados/ListaEmpleados').then((m) => ({ default: m.ListaEmpleados })))
const Feriados = lazy(() => import('./paginas/organizacion/Feriados').then((m) => ({ default: m.Feriados })))
const Organizacion = lazy(() => import('./paginas/organizacion/Organizacion').then((m) => ({ default: m.Organizacion })))
const PlanesSeguro = lazy(() => import('./paginas/seguros/PlanesSeguro').then((m) => ({ default: m.PlanesSeguro })))
const Aprobaciones = lazy(() => import('./paginas/vacaciones/Aprobaciones').then((m) => ({ default: m.Aprobaciones })))
const MisVacaciones = lazy(() => import('./paginas/vacaciones/MisVacaciones').then((m) => ({ default: m.MisVacaciones })))

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // No reintentar errores de permisos o validación: solo fallas de red o del servidor.
      retry: (intentos, error) => !(error instanceof ErrorApi && error.estado < 500) && intentos < 2,
      refetchOnWindowFocus: false,
    },
  },
})

const GESTOR: Rol[] = ['Admin', 'RRHH']

const protegida = (elemento: ReactNode, roles?: Rol[]) => <RutaProtegida roles={roles}>{elemento}</RutaProtegida>

const router = createBrowserRouter([
  { path: '/login', element: <Login /> },
  {
    path: '/',
    element: protegida(<Layout />),
    children: [
      { index: true, element: <Inicio /> },
      { path: 'empleados', element: protegida(<ListaEmpleados />, ['Admin', 'RRHH', 'Jefatura']) },
      { path: 'empleados/nuevo', element: protegida(<FormularioEmpleado />, GESTOR) },
      { path: 'empleados/:id', element: <DetalleEmpleado /> },
      { path: 'empleados/:id/editar', element: protegida(<FormularioEmpleado />, GESTOR) },
      { path: 'vacaciones', element: <MisVacaciones /> },
      { path: 'vacaciones/aprobaciones', element: protegida(<Aprobaciones />, ['Admin', 'RRHH', 'Jefatura']) },
      { path: 'feriados', element: protegida(<Feriados />, GESTOR) },
      { path: 'organizacion', element: protegida(<Organizacion />, GESTOR) },
      { path: 'seguros', element: protegida(<PlanesSeguro />, GESTOR) },
      { path: 'usuarios', element: protegida(<Usuarios />, ['Admin']) },
      { path: 'auditoria', element: protegida(<Auditoria />, ['Admin']) },
      { path: 'cuenta', element: <MiCuenta /> },
      { path: '*', element: <NoEncontrado /> },
    ],
  },
])

function NoEncontrado() {
  return (
    <div className="vacio">
      <h1>Página no encontrada</h1>
      <Link to="/">Volver al inicio</Link>
    </div>
  )
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>
  )
}
