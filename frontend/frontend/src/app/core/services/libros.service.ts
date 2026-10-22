import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, Libro, LibroDisponibilidad } from '../models/biblioteca.models';

@Injectable({
  providedIn: 'root'
})
export class LibrosService {
  private readonly apiUrl = 'http://localhost:5000/api/libros';

  constructor(private http: HttpClient) {}

  getAll(filters?: {
    search?: string;
    autorId?: number;
    categoriaId?: number;
    disponible?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<Libro>> {
    let params = new HttpParams();
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.autorId) params = params.set('autorId', filters.autorId.toString());
    if (filters?.categoriaId) params = params.set('categoriaId', filters.categoriaId.toString());
    if (filters?.disponible !== undefined) params = params.set('disponible', filters.disponible.toString());
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<PagedResult<Libro>>(this.apiUrl, { params });
  }

  getDisponibles(): Observable<Libro[]> {
    return this.http.get<Libro[]>(`${this.apiUrl}/disponibles`);
  }

  getById(id: number): Observable<Libro> {
    return this.http.get<Libro>(`${this.apiUrl}/${id}`);
  }

  getDisponibilidad(id: number): Observable<LibroDisponibilidad> {
    return this.http.get<LibroDisponibilidad>(`${this.apiUrl}/${id}/disponibilidad`);
  }

  create(libro: any): Observable<Libro> {
    return this.http.post<Libro>(this.apiUrl, libro);
  }

  update(id: number, libro: any): Observable<Libro> {
    return this.http.put<Libro>(`${this.apiUrl}/${id}`, libro);
  }

  patchDisponibilidad(id: number, disponible: boolean): Observable<Libro> {
    return this.http.patch<Libro>(`${this.apiUrl}/${id}/disponibilidad`, { disponible });
  }

  uploadPortada(id: number, file: File): Observable<Libro> {
    const formData = new FormData();
    formData.append('archivo', file);
    return this.http.post<Libro>(`${this.apiUrl}/${id}/portada`, formData);
  }

  deletePortada(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}/portada`);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
