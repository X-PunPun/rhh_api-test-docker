import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { z } from 'zod'
import { useAuth } from '../auth/contexto'
import { Aviso, Campo } from '../comun/componentes/Basicos'
import { mensajeError } from '../comun/formato'

const esquema = z.object({
  email: z.email('Ingrese un email válido.'),
  clave: z.string().min(1, 'Ingrese su clave.'),
})

type Datos = z.infer<typeof esquema>

const USUARIOS_DEMO = [
  ['carolina.fuentes@empresa-demo.cl', 'Admin'],
  ['marcela.soto@empresa-demo.cl', 'RRHH nacional'],
  ['valentina.reyes@empresa-demo.cl', 'RRHH Valparaíso'],
  ['rodrigo.vergara@empresa-demo.cl', 'Jefatura'],
  ['matias.gonzalez@empresa-demo.cl', 'Empleado'],
] as const

export function Login() {
  const { usuario, iniciarSesion } = useAuth()
  const navigate = useNavigate()
  const ubicacion = useLocation()
  const [error, setError] = useState<string | null>(null)
  const { register, handleSubmit, setValue, formState: { errors, isSubmitting } } = useForm<Datos>({
    resolver: zodResolver(esquema),
  })

  if (usuario) return <Navigate to="/" replace />

  const destino = (ubicacion.state as { desde?: string } | null)?.desde ?? '/'

  const enviar = handleSubmit(async ({ email, clave }) => {
    setError(null)
    try {
      await iniciarSesion(email, clave)
      navigate(destino, { replace: true })
    } catch (e) {
      setError(mensajeError(e))
    }
  })

  return (
    <div className="login">
      <section className="login-panel">
        <div className="marca">
          <span className="marca-logo" aria-hidden="true">RH</span>
          <span>Gestión de personas</span>
        </div>
        <h1>Iniciar sesión</h1>

        <form onSubmit={enviar} noValidate>
          <Campo etiqueta="Email corporativo" id="email" error={errors.email?.message}>
            <input id="email" type="email" autoComplete="username" {...register('email')} />
          </Campo>
          <Campo etiqueta="Clave" id="clave" error={errors.clave?.message}>
            <input id="clave" type="password" autoComplete="current-password" {...register('clave')} />
          </Campo>
          <Aviso>{error}</Aviso>
          <button className="boton boton-primario ancho-completo" disabled={isSubmitting}>
            {isSubmitting ? 'Ingresando…' : 'Ingresar'}
          </button>
        </form>

        {import.meta.env.DEV && (
          <details className="usuarios-demo">
            <summary>Usuarios de demostración (clave: Demo.Rrhh2026)</summary>
            <ul>
              {USUARIOS_DEMO.map(([email, rol]) => (
                <li key={email}>
                  <button type="button" className="enlace" onClick={() => {
                    setValue('email', email)
                    setValue('clave', 'Demo.Rrhh2026')
                  }}>{email}</button>
                  <span className="texto-secundario"> {rol}</span>
                </li>
              ))}
            </ul>
          </details>
        )}
      </section>
    </div>
  )
}
