export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface UserSession {
  token: string;
  email: string;
  nombreCompleto: string;
  rol: string;
  permisos: string[];
  lectorId?: number | null;
  expiracion: string;
}

export interface UserSummary {
  id: string;
  email: string;
  nombreCompleto: string;
  rol: string;
  permisos: string[];
  lectorId?: number | null;
}

export interface Autor {
  id: number;
  nombre: string;
  apellido: string;
  nombreCompleto: string;
  nacionalidad?: string | null;
  biografia?: string | null;
  fechaNacimiento?: string | null;
  cantidadLibros: number;
  rowVersion?: string | null;
}

export interface Categoria {
  id: number;
  nombre: string;
  descripcion?: string | null;
  activa: boolean;
  cantidadLibros: number;
  rowVersion?: string | null;
}

export interface Libro {
  id: number;
  titulo: string;
  isbn: string;
  resumen?: string | null;
  anioPublicacion: number;
  editorial?: string | null;
  disponible: boolean;
  portadaUrl?: string | null;
  autorId: number;
  autorNombre: string;
  categoriaId: number;
  categoriaNombre: string;
  rowVersion?: string | null;
}

export interface LibroDisponibilidad {
  libroId: number;
  titulo: string;
  disponible: boolean;
  prestamoActivoId?: number | null;
  lectorPrestamoNombre?: string | null;
  fechaVencimiento?: string | null;
}

export interface Lector {
  id: number;
  nombreCompleto: string;
  documentoIdentidad: string;
  email: string;
  telefono?: string | null;
  direccion?: string | null;
  activo: boolean;
  usuarioId?: string | null;
  fechaRegistro: string;
  prestamosActivosCount: number;
  rowVersion?: string | null;
}

export interface Prestamo {
  id: number;
  libroId: number;
  libroTitulo: string;
  libroISBN: string;
  libroPortadaUrl?: string | null;
  lectorId: number;
  lectorNombreCompleto: string;
  lectorDocumento: string;
  fechaPrestamo: string;
  fechaVencimiento: string;
  fechaDevolucionReal?: string | null;
  estado: 'Activo' | 'Devuelto' | 'Cancelado' | 'Vencido';
  observaciones?: string | null;
  estaVencido: boolean;
  rowVersion?: string | null;
}
