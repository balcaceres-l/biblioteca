import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { UserSession } from '../models/biblioteca.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5000/api/auth';
  private readonly storageKey = 'biblioteca_session';

  private readonly sessionSignal = signal<UserSession | null>(this.loadStoredSession());

  public readonly session = this.sessionSignal.asReadonly();
  public readonly isLoggedIn = computed(() => this.sessionSignal() !== null);
  public readonly currentUser = computed(() => this.sessionSignal()?.nombreCompleto ?? '');
  public readonly currentRole = computed(() => this.sessionSignal()?.rol ?? '');
  public readonly lectorId = computed(() => this.sessionSignal()?.lectorId ?? null);

  public readonly isAdmin = computed(() => this.sessionSignal()?.rol === 'Administrador');
  public readonly isBibliotecario = computed(() => this.sessionSignal()?.rol === 'Bibliotecario');
  public readonly isLector = computed(() => this.sessionSignal()?.rol === 'Lector');

  constructor(private http: HttpClient) {}

  login(credentials: { email: string; password: string }): Observable<UserSession> {
    return this.http.post<UserSession>(`${this.apiUrl}/login`, credentials).pipe(
      tap((session) => this.setSession(session))
    );
  }

  register(data: any): Observable<UserSession> {
    return this.http.post<UserSession>(`${this.apiUrl}/register`, data).pipe(
      tap((session) => this.setSession(session))
    );
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.sessionSignal.set(null);
  }

  getToken(): string | null {
    return this.sessionSignal()?.token ?? null;
  }

  hasRole(roles: string[]): boolean {
    const rolActual = this.currentRole();
    return roles.includes(rolActual);
  }

  hasPermission(permiso: string): boolean {
    const permisos = this.sessionSignal()?.permisos ?? [];
    return permisos.includes(permiso);
  }

  private setSession(session: UserSession): void {
    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.sessionSignal.set(session);
  }

  private loadStoredSession(): UserSession | null {
    try {
      const raw = localStorage.getItem(this.storageKey);
      if (!raw) return null;
      const parsed: UserSession = JSON.parse(raw);
      if (new Date(parsed.expiracion) <= new Date()) {
        localStorage.removeItem(this.storageKey);
        return null;
      }
      return parsed;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }
}
