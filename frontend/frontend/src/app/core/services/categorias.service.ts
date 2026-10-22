import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, Categoria, Libro } from '../models/biblioteca.models';

@Injectable({
  providedIn: 'root'
})
export class CategoriasService {
  private readonly apiUrl = 'http://localhost:5000/api/categorias';

  constructor(private http: HttpClient) {}

  getAll(filters?: { search?: string; activa?: boolean; page?: number; pageSize?: number }): Observable<PagedResult<Categoria>> {
    let params = new HttpParams();
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.activa !== undefined) params = params.set('activa', filters.activa.toString());
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<PagedResult<Categoria>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<Categoria> {
    return this.http.get<Categoria>(`${this.apiUrl}/${id}`);
  }

  getLibrosByCategoria(id: number): Observable<Libro[]> {
    return this.http.get<Libro[]>(`${this.apiUrl}/${id}/libros`);
  }

  create(categoria: any): Observable<Categoria> {
    return this.http.post<Categoria>(this.apiUrl, categoria);
  }

  update(id: number, categoria: any): Observable<Categoria> {
    return this.http.put<Categoria>(`${this.apiUrl}/${id}`, categoria);
  }

  patchEstado(id: number, activa: boolean): Observable<Categoria> {
    return this.http.patch<Categoria>(`${this.apiUrl}/${id}/estado`, { activa });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
