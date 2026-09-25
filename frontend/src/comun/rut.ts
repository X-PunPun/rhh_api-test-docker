/** Utilidades de RUT chileno (mismo algoritmo módulo 11 que el dominio en .NET). */

export function calcularDv(numero: number): string {
  let suma = 0
  let multiplicador = 2
  let resto = numero

  while (resto > 0) {
    suma += (resto % 10) * multiplicador
    resto = Math.floor(resto / 10)
    multiplicador = multiplicador === 7 ? 2 : multiplicador + 1
  }

  const dv = 11 - (suma % 11)
  return dv === 11 ? '0' : dv === 10 ? 'K' : String(dv)
}

export function limpiarRut(valor: string): string {
  return valor.replace(/[.\-\s]/g, '').toUpperCase()
}

export function esRutValido(valor: string): boolean {
  const limpio = limpiarRut(valor)
  if (!/^\d{1,8}[\dK]$/.test(limpio)) return false
  const numero = Number(limpio.slice(0, -1))
  return numero > 0 && calcularDv(numero) === limpio.slice(-1)
}

/** "123456785" → "12.345.678-5" (mientras se escribe, formatea lo que haya). */
export function formatearRut(valor: string): string {
  const limpio = limpiarRut(valor).slice(0, 9)
  if (limpio.length < 2) return limpio
  const cuerpo = limpio.slice(0, -1).replace(/\B(?=(\d{3})+(?!\d))/g, '.')
  return `${cuerpo}-${limpio.slice(-1)}`
}
