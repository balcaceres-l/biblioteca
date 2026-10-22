import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, Prestamo } from '../models/biblioteca.models';

@Injectable({
  providedIn: 'root'
})
export class PrestamosService {
  private readonly apiUrl = 'http://localhost:5000/api/prestamos';

  constructor(private http: HttpClient) {}

  getAll(filters?: {
    estado?: string;
    lectorId?: number;
    libroId?: number;
    soloVencidos?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<Prestamo>> {
    let params = new HttpParams();
    if (filters?.estado) params = params.set('estado', filters.estado);
    if (filters?.lectorId) params = params.set('lectorId', filters.lectorId.toString());
    if (filters?.libroId) params = params.set('libroId', filters.libroId.toString());
    if (filters?.soloVencidos !== undefined) params = params.set('soloVencidos', filters.soloVencidos.toString());
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<PagedResult<Prestamo>>(this.apiUrl, { params });
  }

  getActivos(page = 1, pageSize = 10): Observable<PagedResult<Prestamo>> {
    const params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<Prestamo>>(`${this.apiUrl}/activos`, { params });
  }

  getVencidos(page = 1, pageSize = 10): Observable<PagedResult<Prestamo>> {
    const params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<Prestamo>>(`${this.apiUrl}/vencidos`, { params });
  }

  getById(id: number): Observable<Prestamo> {
    return this.http.get<Prestamo>(`${this.apiUrl}/${id}`);
  }

  registrarPrestamo(data: {
    libroId: number;
    lectorId?: number;
    diasPrestamo?: number;
    observaciones?: string;
    libroRowVersion?: string;
  }): Observable<Prestamo> {
    return this.http.post<Prestamo>(this.apiUrl, data);
  }

  registrarDevolucion(id: number, observaciones?: string): Observable<Prestamo> {
    return this.http.patch<Prestamo>(`${this.apiUrl}/${id}/devolucion`, { observaciones });
  }

  cancelarPrestamo(id: number, motivo: string): Observable<Prestamo> {
    return this.http.patch<Prestamo>(`${this.apiUrl}/${id}/cancelar`, { motivo });
  }
}
