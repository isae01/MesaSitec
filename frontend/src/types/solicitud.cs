// Estos types son literalmente el contrato de la API que ya probamos en Swagger.
// Como cuando armas los types de la respuesta de tu API en un proyecto de React.

export type Rol = 'Admin' | 'Agente' | 'Solicitante'
export type Prioridad = 'Baja' | 'Media' | 'Alta' | 'Critica'
export type EstadoSolicitud = 'Nueva' | 'Asignada' | 'EnProceso' | 'Resuelta' | 'Cerrada' | 'Cancelada'

export interface Usuario {
  id: string
  nombre: string
  email: string
  rol: Rol
  tenantId: string
  tenantNombre: string
}

export interface CategoriaResumen {
  id: string
  nombre: string
}

export interface AgenteResumen {
  id: string
  nombre: string
}

export interface SolicitudListItem {
  id: string
  codigo: string
  titulo: string
  estado: EstadoSolicitud
  prioridad: Prioridad
  categoria: CategoriaResumen
  agente: AgenteResumen | null
  fechaCreacion: string
  fechaLimiteSla: string
  vencida: boolean
}

export interface SolicitudesPaginadas {
  items: SolicitudListItem[]
  page: number
  pageSize: number
  total: number
  totalPaginas: number
}

export interface SolicitudDetalle extends SolicitudListItem {
  descripcion: string
  solicitante: { id: string; nombre: string }
  fechaResolucion: string | null
  motivoResolucion: string | null
  motivoCancelacion: string | null
}

// La forma exacta del error que definimos en el backend (application/problem+json)
export interface ApiError {
  type: string
  title: string
  status: number
  detail: string
  codigo: string
  errores?: Record<string, string[]>
}