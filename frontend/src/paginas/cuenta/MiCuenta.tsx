import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { authApi } from '../../api/endpoints'
import { useAuth } from '../../auth/contexto'
import { Aviso, Campo, EncabezadoPagina } from '../../comun/componentes/Basicos'
import { mensajeError } from '../../comun/formato'

export function MiCuenta() {
  const { usuario, cerrarSesion } = useAuth()
  const navigate = useNavigate()
  const [actual, setActual] = useState('')
  const [nueva, setNueva] = useState('')
  const [confirmacion, setConfirmacion] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const enviar = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    if (nueva !== confirmacion) {
      setError('La confirmación no coincide con la clave nueva.')
      return
    }
    setEnviando(true)
    try {
      await authApi.cambiarClave(actual, nueva)
      // Cambiar la clave cierra todas las sesiones: se vuelve a ingresar con la clave nueva.
      await cerrarSesion()
      navigate('/login', { replace: true })
    } catch (err) {
      setError(mensajeError(err))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <>
      <EncabezadoPagina titulo="Mi cuenta" descripcion={`${usuario?.email} · rol ${usuario?.rol}`} />
      <section className="tarjeta angosta">
        <h2>Cambiar clave</h2>
        <p className="texto-secundario">Mínimo 10 caracteres con mayúscula, minúscula, número y símbolo. Al cambiarla se cierran todas sus sesiones.</p>
        <form className="formulario" onSubmit={enviar}>
          <Campo etiqueta="Clave actual" id="clave-actual">
            <input id="clave-actual" type="password" required autoComplete="current-password" value={actual} onChange={(e) => setActual(e.target.value)} />
          </Campo>
          <Campo etiqueta="Clave nueva" id="clave-nueva">
            <input id="clave-nueva" type="password" required minLength={10} autoComplete="new-password" value={nueva} onChange={(e) => setNueva(e.target.value)} />
          </Campo>
          <Campo etiqueta="Confirmar clave nueva" id="clave-confirmacion">
            <input id="clave-confirmacion" type="password" required autoComplete="new-password" value={confirmacion} onChange={(e) => setConfirmacion(e.target.value)} />
          </Campo>
          <Aviso>{error}</Aviso>
          <div className="acciones"><button className="boton boton-primario" disabled={enviando}>Cambiar clave</button></div>
        </form>
      </section>
    </>
  )
}
