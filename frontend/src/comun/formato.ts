const fechaCorta = new Intl.DateTimeFormat('es-CL', { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' })
const fechaHora = new Intl.DateTimeFormat('es-CL', { dateStyle: 'short', timeStyle: 'short' })
const numero = new Intl.NumberFormat('es-CL', { maximumFractionDigits: 2 })
const uf = new Intl.NumberFormat('es-CL', { minimumFractionDigits: 2, maximumFractionDigits: 4 })

/** "2026-09-24" → "24-09-2026" (las fechas sin hora se tratan en UTC para no correrse un día). */
export function fecha(iso: string | null | undefined): string {
  if (!iso) return '—'
  return fechaCorta.format(new Date(iso.length === 10 ? `${iso}T00:00:00Z` : iso)).replaceAll('/', '-')
}

export function fechaYHora(iso: string | null | undefined): string {
  return iso ? fechaHora.format(new Date(iso)) : '—'
}

export function num(valor: number): string {
  return numero.format(valor)
}

export function formatoUf(valor: number): string {
  return `${uf.format(valor)} UF`
}

/** Fecha local de hoy en formato ISO (yyyy-mm-dd) para inputs type="date". */
export function hoyIso(): string {
  const hoy = new Date()
  const mes = String(hoy.getMonth() + 1).padStart(2, '0')
  const dia = String(hoy.getDate()).padStart(2, '0')
  return `${hoy.getFullYear()}-${mes}-${dia}`
}

export function mensajeError(error: unknown): string {
  return error instanceof Error ? error.message : 'Ocurrió un error inesperado.'
}
