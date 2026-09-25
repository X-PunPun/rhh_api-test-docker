/** Cuenta días hábiles (lunes a viernes, sin feriados) igual que la API, para mostrar una vista previa. */
export function contarDiasHabiles(inicio: string, fin: string, feriados: Set<string>): number {
  if (!inicio || !fin || fin < inicio) return 0
  let dias = 0
  for (let d = new Date(`${inicio}T00:00:00Z`); d <= new Date(`${fin}T00:00:00Z`); d.setUTCDate(d.getUTCDate() + 1)) {
    const dia = d.getUTCDay()
    if (dia !== 0 && dia !== 6 && !feriados.has(d.toISOString().slice(0, 10))) dias++
  }
  return dias
}
