import { useEffect, useRef, type ReactNode } from 'react'
import type { EstadoSolicitud } from '../../api/tipos'

export function EncabezadoPagina({ titulo, descripcion, acciones }: { titulo: string; descripcion?: string; acciones?: ReactNode }) {
  return (
    <header className="encabezado-pagina">
      <div>
        <h1>{titulo}</h1>
        {descripcion && <p className="texto-secundario">{descripcion}</p>}
      </div>
      {acciones && <div className="acciones">{acciones}</div>}
    </header>
  )
}

export function Aviso({ tipo = 'error', children }: { tipo?: 'error' | 'exito' | 'info'; children: ReactNode }) {
  if (!children) return null
  return (
    <div className={`aviso aviso-${tipo}`} role={tipo === 'error' ? 'alert' : 'status'}>
      {children}
    </div>
  )
}

export function Cargando({ texto = 'Cargando…' }: { texto?: string }) {
  return <p className="cargando" aria-live="polite">{texto}</p>
}

export function Vacio({ children }: { children: ReactNode }) {
  return <p className="vacio">{children}</p>
}

/** Campo de formulario: etiqueta + control + mensaje de error asociado. */
export function Campo({ etiqueta, error, ayuda, children, id }: {
  etiqueta: string
  id: string
  error?: string
  ayuda?: string
  children: ReactNode
}) {
  return (
    <div className={`campo${error ? ' campo-error' : ''}`}>
      <label htmlFor={id}>{etiqueta}</label>
      {children}
      {ayuda && !error && <small className="texto-secundario">{ayuda}</small>}
      {error && <small className="error" id={`${id}-error`}>{error}</small>}
    </div>
  )
}

const CLASE_ESTADO: Record<EstadoSolicitud, string> = {
  Pendiente: 'pill-advertencia',
  Aprobada: 'pill-ok',
  Rechazada: 'pill-critico',
  Cancelada: 'pill-neutro',
}

export function PillEstado({ estado }: { estado: EstadoSolicitud }) {
  return <span className={`pill ${CLASE_ESTADO[estado]}`}>{estado}</span>
}

export function PillActivo({ activo, textoActivo = 'Activo', textoInactivo = 'Inactivo' }: {
  activo: boolean
  textoActivo?: string
  textoInactivo?: string
}) {
  return <span className={`pill ${activo ? 'pill-ok' : 'pill-neutro'}`}>{activo ? textoActivo : textoInactivo}</span>
}

export function Paginacion({ pagina, totalPaginas, total, onCambiar }: {
  pagina: number
  totalPaginas: number
  total: number
  onCambiar: (pagina: number) => void
}) {
  if (total === 0) return null
  return (
    <nav className="paginacion" aria-label="Paginación">
      <span className="texto-secundario">{total} registro(s) · página {pagina} de {Math.max(totalPaginas, 1)}</span>
      <div className="acciones">
        <button className="boton boton-secundario" disabled={pagina <= 1} onClick={() => onCambiar(pagina - 1)}>Anterior</button>
        <button className="boton boton-secundario" disabled={pagina >= totalPaginas} onClick={() => onCambiar(pagina + 1)}>Siguiente</button>
      </div>
    </nav>
  )
}

/** Diálogo modal accesible basado en <dialog> nativo (foco atrapado y Escape para cerrar). */
export function Dialogo({ abierto, titulo, onCerrar, children }: {
  abierto: boolean
  titulo: string
  onCerrar: () => void
  children: ReactNode
}) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialogo = ref.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  return (
    <dialog ref={ref} className="dialogo" onClose={onCerrar} aria-labelledby="dialogo-titulo">
      <header>
        <h2 id="dialogo-titulo">{titulo}</h2>
        <button className="boton-icono" onClick={onCerrar} aria-label="Cerrar">×</button>
      </header>
      {abierto && children}
    </dialog>
  )
}

export function Indicador({ etiqueta, valor, detalle }: { etiqueta: string; valor: string | number; detalle?: string }) {
  return (
    <div className="indicador">
      <span className="indicador-etiqueta">{etiqueta}</span>
      <strong className="indicador-valor">{valor}</strong>
      {detalle && <span className="texto-secundario">{detalle}</span>}
    </div>
  )
}
