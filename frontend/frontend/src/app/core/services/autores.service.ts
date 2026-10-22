import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, Autor, Libro } from '../models/biblioteca.models';

@Injectable({
  providedIn: 'root'
})
export class AutoresService {
  private readonly apiUrl = 'http://localhost:5000/api/autores';

  constructor(private http: HttpClient) {}

  getAll(filters?: { search?: string; page?: number; pageSize?: number }): Observable<PagedResult<Autor>> {
    let params = new HttpParams();
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http.get<PagedResult<Autor>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<Autor> {
    return this.http.get<Autor>(`${this.apiUrl}/${id}`);
  }

  getLibrosByAutor(id: number): Observable<Libro[]> {
    return this.http.get<Libro[]>(`${this.apiUrl}/${id}/libros`);
  }

  create(autor: any): Observable<Autor> {
    return this.http.post<Autor>(this.apiUrl, autor);
  }

  update(id: number, autor: any): Observable<Autor> {
    return this.http.put<Autor>(`${this.apiUrl}/${id}`, autor);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
