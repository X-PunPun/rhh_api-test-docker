import { contarDiasHabiles } from '../comun/vacaciones'

describe('contarDiasHabiles (vista previa de la solicitud)', () => {
  it('cuenta lunes a viernes', () => {
    expect(contarDiasHabiles('2026-11-02', '2026-11-08', new Set())).toBe(5)
  })

  it('descuenta feriados', () => {
    expect(contarDiasHabiles('2026-09-14', '2026-09-18', new Set(['2026-09-18']))).toBe(4)
  })

  it('fin de semana o rango inválido da cero', () => {
    expect(contarDiasHabiles('2026-11-07', '2026-11-08', new Set())).toBe(0)
    expect(contarDiasHabiles('2026-11-10', '2026-11-02', new Set())).toBe(0)
  })
})
