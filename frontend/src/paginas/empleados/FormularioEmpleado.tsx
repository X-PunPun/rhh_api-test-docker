import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useForm, useWatch } from 'react-hook-form'
import { useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { empleadosApi } from '../../api/endpoints'
import { AFPS, SISTEMAS_SALUD, type EmpleadoDetalle } from '../../api/tipos'
import { Aviso, Campo, Cargando, EncabezadoPagina } from '../../comun/componentes/Basicos'
import { useCargos, useComunas, useDepartamentos, useRegiones } from '../../comun/consultas'
import { mensajeError } from '../../comun/formato'
import { esRutValido, formatearRut } from '../../comun/rut'

// Validación en el navegador para dar respuesta inmediata; la API vuelve a validar todo.
const esquema = z.object({
  rut: z.string().refine(esRutValido, 'RUT inválido (revise el dígito verificador).'),
  nombres: z.string().trim().min(1, 'Obligatorio.').max(100),
  apellidoPaterno: z.string().trim().min(1, 'Obligatorio.').max(100),
  apellidoMaterno: z.string().trim().max(100),
  email: z.email('Email inválido.').max(150),
  fechaNacimiento: z.string().min(1, 'Obligatorio.'),
  fechaIngreso: z.string().min(1, 'Obligatorio.'),
  regionId: z.string().min(1, 'Seleccione una región.'),
  comunaId: z.string().min(1, 'Seleccione una comuna.'),
  departamentoId: z.string().min(1, 'Seleccione un departamento.'),
  cargoId: z.string().min(1, 'Seleccione un cargo.'),
  jefeId: z.string(),
  afp: z.enum(AFPS, 'Seleccione una AFP.'),
  sistemaSalud: z.enum(SISTEMAS_SALUD, 'Seleccione el sistema de salud.'),
  aniosServicioPrevios: z.coerce.number<string>().int().min(0).max(60),
}).refine((d) => {
  const nacimiento = new Date(d.fechaNacimiento)
  const ingreso = new Date(d.fechaIngreso)
  nacimiento.setFullYear(nacimiento.getFullYear() + 18)
  return ingreso >= nacimiento
}, { path: ['fechaIngreso'], message: 'Debe tener al menos 18 años a la fecha de ingreso.' })

type Entrada = z.input<typeof esquema>
type Datos = z.output<typeof esquema>

function valoresIniciales(e?: EmpleadoDetalle): Entrada {
  return {
    rut: e?.rut ?? '',
    nombres: e?.nombres ?? '',
    apellidoPaterno: e?.apellidoPaterno ?? '',
    apellidoMaterno: e?.apellidoMaterno ?? '',
    email: e?.email ?? '',
    fechaNacimiento: e?.fechaNacimiento ?? '',
    fechaIngreso: e?.fechaIngreso ?? '',
    regionId: e ? String(e.region.id) : '',
    comunaId: e ? String(e.comuna.id) : '',
    departamentoId: e ? String(e.departamento.id) : '',
    cargoId: e ? String(e.cargo.id) : '',
    jefeId: e?.jefe ? String(e.jefe.id) : '',
    afp: e?.afp ?? ('' as Entrada['afp']),
    sistemaSalud: e?.sistemaSalud ?? ('' as Entrada['sistemaSalud']),
    aniosServicioPrevios: String(e?.aniosServicioPrevios ?? 0),
  }
}

export function FormularioEmpleado() {
  const { id } = useParams()
  const editando = id !== undefined
  const existente = useQuery({
    queryKey: ['empleado', Number(id)],
    queryFn: () => empleadosApi.obtener(Number(id)),
    enabled: editando,
  })

  // Los catálogos se cargan antes de dibujar el formulario: así los <select> ya tienen sus opciones
  // cuando se aplican los valores iniciales (al editar).
  const regiones = useRegiones()
  const departamentos = useDepartamentos(true)
  const jefes = usePosiblesJefes()
  const comunas = useComunas(existente.data?.region.id)
  const cargos = useCargos(existente.data?.departamento.id, true)

  if (editando && existente.error) return <Aviso>{mensajeError(existente.error)}</Aviso>

  const listo = regiones.data && departamentos.data && jefes.data &&
    (!editando || (existente.data && comunas.data && cargos.data))

  return listo ? <Formulario empleado={existente.data} /> : <Cargando />
}

function usePosiblesJefes() {
  return useQuery({
    queryKey: ['empleados', 'posibles-jefes'],
    queryFn: () => empleadosApi.buscar({ activo: true, tamanoPagina: 100 }),
  })
}

function Formulario({ empleado }: { empleado?: EmpleadoDetalle }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const editando = empleado !== undefined

  const { register, handleSubmit, control, setValue, formState: { errors } } = useForm<Entrada, unknown, Datos>({
    resolver: zodResolver(esquema),
    defaultValues: valoresIniciales(empleado),
  })

  const regionId = useWatch({ control, name: 'regionId' })
  const departamentoId = useWatch({ control, name: 'departamentoId' })

  const regiones = useRegiones()
  const comunas = useComunas(regionId ? Number(regionId) : undefined)
  const departamentos = useDepartamentos(true)
  const cargos = useCargos(departamentoId ? Number(departamentoId) : undefined, true)
  const jefes = usePosiblesJefes()

  const guardar = useMutation({
    mutationFn: (d: Datos) => {
      const comunes = {
        email: d.email,
        comunaId: Number(d.comunaId),
        departamentoId: Number(d.departamentoId),
        cargoId: Number(d.cargoId),
        jefeId: d.jefeId ? Number(d.jefeId) : null,
        afp: d.afp,
        sistemaSalud: d.sistemaSalud,
        aniosServicioPrevios: d.aniosServicioPrevios,
      }
      return editando
        ? empleadosApi.actualizar(empleado.id, comunes)
        : empleadosApi.crear({
            ...comunes,
            rut: d.rut,
            nombres: d.nombres,
            apellidoPaterno: d.apellidoPaterno,
            apellidoMaterno: d.apellidoMaterno || null,
            fechaNacimiento: d.fechaNacimiento,
            fechaIngreso: d.fechaIngreso,
          })
    },
    onSuccess: async (guardado) => {
      await queryClient.invalidateQueries({ queryKey: ['empleados'] })
      queryClient.setQueryData(['empleado', guardado.id], guardado)
      navigate(`/empleados/${guardado.id}`)
    },
  })

  const rut = register('rut')

  return (
    <>
      <EncabezadoPagina
        titulo={editando ? `Editar a ${empleado.nombres} ${empleado.apellidoPaterno}` : 'Nuevo empleado'}
        descripcion={editando ? 'RUT, nombre y fechas no se modifican desde aquí.' : 'Todos los campos, salvo el apellido materno y la jefatura, son obligatorios.'}
      />

      <form className="formulario tarjeta" onSubmit={handleSubmit((d) => guardar.mutate(d))} noValidate>
        <fieldset>
          <legend>Datos personales</legend>
          <div className="grilla-campos">
            <Campo etiqueta="RUT" id="rut" error={errors.rut?.message} ayuda="Ej.: 12.345.678-5">
              <input id="rut" className="mono" disabled={editando} {...rut}
                onChange={(e) => { e.target.value = formatearRut(e.target.value); void rut.onChange(e) }} />
            </Campo>
            <Campo etiqueta="Nombres" id="nombres" error={errors.nombres?.message}>
              <input id="nombres" disabled={editando} {...register('nombres')} />
            </Campo>
            <Campo etiqueta="Apellido paterno" id="apellidoPaterno" error={errors.apellidoPaterno?.message}>
              <input id="apellidoPaterno" disabled={editando} {...register('apellidoPaterno')} />
            </Campo>
            <Campo etiqueta="Apellido materno" id="apellidoMaterno" error={errors.apellidoMaterno?.message}>
              <input id="apellidoMaterno" disabled={editando} {...register('apellidoMaterno')} />
            </Campo>
            <Campo etiqueta="Fecha de nacimiento" id="fechaNacimiento" error={errors.fechaNacimiento?.message}>
              <input id="fechaNacimiento" type="date" disabled={editando} {...register('fechaNacimiento')} />
            </Campo>
            <Campo etiqueta="Email" id="email" error={errors.email?.message}>
              <input id="email" type="email" {...register('email')} />
            </Campo>
          </div>
        </fieldset>

        <fieldset>
          <legend>Ubicación</legend>
          <div className="grilla-campos">
            <Campo etiqueta="Región" id="regionId" error={errors.regionId?.message}>
              <select id="regionId" {...register('regionId', { onChange: () => setValue('comunaId', '') })}>
                <option value="">Seleccione…</option>
                {regiones.data?.map((r) => <option key={r.id} value={r.id}>{r.abreviatura} · {r.nombre}</option>)}
              </select>
            </Campo>
            <Campo etiqueta="Comuna" id="comunaId" error={errors.comunaId?.message}>
              <select id="comunaId" disabled={!regionId} {...register('comunaId')}>
                <option value="">Seleccione…</option>
                {comunas.data?.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
              </select>
            </Campo>
          </div>
        </fieldset>

        <fieldset>
          <legend>Contrato y organización</legend>
          <div className="grilla-campos">
            <Campo etiqueta="Fecha de ingreso" id="fechaIngreso" error={errors.fechaIngreso?.message}>
              <input id="fechaIngreso" type="date" disabled={editando} {...register('fechaIngreso')} />
            </Campo>
            <Campo etiqueta="Departamento" id="departamentoId" error={errors.departamentoId?.message}>
              <select id="departamentoId" {...register('departamentoId', { onChange: () => setValue('cargoId', '') })}>
                <option value="">Seleccione…</option>
                {departamentos.data?.map((d) => <option key={d.id} value={d.id}>{d.nombre}</option>)}
              </select>
            </Campo>
            <Campo etiqueta="Cargo" id="cargoId" error={errors.cargoId?.message}>
              <select id="cargoId" disabled={!departamentoId} {...register('cargoId')}>
                <option value="">Seleccione…</option>
                {cargos.data?.map((c) => <option key={c.id} value={c.id}>{c.nombre}</option>)}
              </select>
            </Campo>
            <Campo etiqueta="Jefatura directa" id="jefeId" ayuda="Opcional">
              <select id="jefeId" {...register('jefeId')}>
                <option value="">Sin jefatura</option>
                {jefes.data?.items.filter((j) => j.id !== empleado?.id).map((j) => (
                  <option key={j.id} value={j.id}>{j.nombreCompleto} · {j.cargo}</option>
                ))}
              </select>
            </Campo>
          </div>
        </fieldset>

        <fieldset>
          <legend>Previsión</legend>
          <div className="grilla-campos">
            <Campo etiqueta="AFP" id="afp" error={errors.afp?.message}>
              <select id="afp" {...register('afp')}>
                <option value="">Seleccione…</option>
                {AFPS.map((a) => <option key={a} value={a}>{a}</option>)}
              </select>
            </Campo>
            <Campo etiqueta="Sistema de salud" id="sistemaSalud" error={errors.sistemaSalud?.message}>
              <select id="sistemaSalud" {...register('sistemaSalud')}>
                <option value="">Seleccione…</option>
                {SISTEMAS_SALUD.map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
            </Campo>
            <Campo etiqueta="Años con empleadores anteriores" id="aniosServicioPrevios"
              error={errors.aniosServicioPrevios?.message} ayuda="Para el feriado progresivo (se reconocen hasta 10).">
              <input id="aniosServicioPrevios" type="number" min={0} max={60} {...register('aniosServicioPrevios')} />
            </Campo>
          </div>
        </fieldset>

        <Aviso>{guardar.error && mensajeError(guardar.error)}</Aviso>
        <div className="acciones">
          <button type="button" className="boton boton-secundario" onClick={() => navigate(-1)}>Cancelar</button>
          <button className="boton boton-primario" disabled={guardar.isPending}>
            {guardar.isPending ? 'Guardando…' : editando ? 'Guardar cambios' : 'Crear empleado'}
          </button>
        </div>
      </form>
    </>
  )
}
