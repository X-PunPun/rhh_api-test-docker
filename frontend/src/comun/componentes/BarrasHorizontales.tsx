import { useState } from 'react'
import type { Conteo } from '../../api/tipos'

/**
 * Barras horizontales para una sola serie (dotación por región o departamento).
 * Un solo color (magnitud, no identidad), ordenadas de mayor a menor, con el valor
 * rotulado al final de cada barra y tooltip al pasar el mouse. La tabla equivalente
 * queda disponible para lectores de pantalla.
 */
export function BarrasHorizontales({ titulo, datos, unidad = 'empleados' }: { titulo: string; datos: Conteo[]; unidad?: string }) {
  const [activo, setActivo] = useState<number | null>(null)
  const ordenados = [...datos].sort((a, b) => b.cantidad - a.cantidad)
  const maximo = Math.max(1, ...ordenados.map((d) => d.cantidad))
  const total = ordenados.reduce((suma, d) => suma + d.cantidad, 0)

  return (
    <figure className="grafico">
      <figcaption>
        <strong>{titulo}</strong>
        <span className="texto-secundario">{total} {unidad}</span>
      </figcaption>

      {ordenados.length === 0 ? (
        <p className="vacio">Sin datos para mostrar.</p>
      ) : (
        <ul className="barras" aria-hidden="true">
          {ordenados.map((d) => {
            const porcentaje = total === 0 ? 0 : Math.round((d.cantidad / total) * 100)
            return (
              <li
                key={d.id}
                className={activo === d.id ? 'activa' : undefined}
                onMouseEnter={() => setActivo(d.id)}
                onMouseLeave={() => setActivo(null)}
              >
                <span className="barra-etiqueta" title={d.nombre}>{d.nombre}</span>
                <span className="barra-pista">
                  <span className="barra" style={{ width: `${(d.cantidad / maximo) * 100}%` }} />
                  {activo === d.id && (
                    <span className="tooltip" role="presentation">
                      {d.nombre}: <strong>{d.cantidad}</strong> ({porcentaje}%)
                    </span>
                  )}
                </span>
                <span className="barra-valor">{d.cantidad}</span>
              </li>
            )
          })}
        </ul>
      )}

      <table className="solo-lectores">
        <caption>{titulo}</caption>
        <thead><tr><th>Nombre</th><th>Cantidad</th></tr></thead>
        <tbody>
          {ordenados.map((d) => <tr key={d.id}><td>{d.nombre}</td><td>{d.cantidad}</td></tr>)}
        </tbody>
      </table>
    </figure>
  )
}
