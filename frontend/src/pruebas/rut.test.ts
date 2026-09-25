import { calcularDv, esRutValido, formatearRut } from '../comun/rut'

describe('RUT (módulo 11, igual que el dominio .NET)', () => {
  it.each([
    [11111111, '1'],
    [12345678, '5'],
    [10000013, 'K'],
    [10000004, '0'],
  ])('calcula el dígito verificador de %i → %s', (numero, dv) => {
    expect(calcularDv(numero)).toBe(dv)
  })

  it.each(['12.345.678-5', '123456785', '10.000.013-k', ' 7.654.321-6 '])('acepta %s', (rut) => {
    expect(esRutValido(rut.trim())).toBe(true)
  })

  it.each(['', '12.345.678-9', '12345678-X', '0-0', 'abc'])('rechaza %s', (rut) => {
    expect(esRutValido(rut)).toBe(false)
  })

  it('formatea mientras se escribe', () => {
    expect(formatearRut('123456785')).toBe('12.345.678-5')
    expect(formatearRut('1')).toBe('1')
    expect(formatearRut('10000013k')).toBe('10.000.013-K')
  })
})
