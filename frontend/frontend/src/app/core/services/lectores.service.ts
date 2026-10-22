import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, Lector, Prestamo } from '../models/biblioteca.models';

@Injectable({
  providedIn: 'root'
})
export class LectoresService {
  private readonly apiUrl = 'http://localhost:5000/api/lectores';

  constructor(private http: HttpClient) {}

  getAll(filters?: { search?: string; activo?: boolean; page?: number; pageSize?: number }): Observable<PagedResult<Lector>> {
    let params = new HttpParams();
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.activo !== undefined) params = params.set('activo', filters.activo.toString());
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<PagedResult<Lector>>(this.apiUrl, { params });
  }

  getMiPerfil(): Observable<Lector> {
    return this.http.get<Lector>(`${this.apiUrl}/mi-perfil`);
  }

  getById(id: number): Observable<Lector> {
    return this.http.get<Lector>(`${this.apiUrl}/${id}`);
  }

  getHistorialPrestamos(id: number): Observable<Prestamo[]> {
    return this.http.get<Prestamo[]>(`${this.apiUrl}/${id}/prestamos`);
  }

  getPrestamosActivos(id: number): Observable<Prestamo[]> {
    return this.http.get<Prestamo[]>(`${this.apiUrl}/${id}/prestamos-activos`);
  }

  create(lector: any): Observable<Lector> {
    return this.http.post<Lector>(this.apiUrl, lector);
  }

  update(id: number, lector: any): Observable<Lector> {
    return this.http.put<Lector>(`${this.apiUrl}/${id}`, lector);
  }

  patchEstado(id: number, activo: boolean): Observable<Lector> {
    return this.http.patch<Lector>(`${this.apiUrl}/${id}/estado`, { activo });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
