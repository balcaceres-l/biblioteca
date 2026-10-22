import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-12 col-md-8 col-lg-5">
          <div class="card shadow-sm border-0 rounded-4">
            <div class="card-body p-4 p-md-5">
              <div class="text-center mb-4">
                <i class="bi bi-book-half text-primary display-4"></i>
                <h3 class="fw-bold mt-2">Iniciar Sesión</h3>
                <p class="text-muted">Sistema de Gestión de Biblioteca (.NET 10 & MariaDB)</p>
              </div>

              @if (errorMessage) {
                <div class="alert alert-danger d-flex align-items-center gap-2" role="alert">
                  <i class="bi bi-exclamation-triangle-fill"></i>
                  <div>{{ errorMessage }}</div>
                </div>
              }

              <form (ngSubmit)="onSubmit()" #loginForm="ngForm">
                <div class="mb-3">
                  <label class="form-label fw-semibold">Correo Electrónico</label>
                  <div class="input-group">
                    <span class="input-group-text"><i class="bi bi-envelope"></i></span>
                    <input
                      type="email"
                      class="form-control"
                      name="email"
                      [(ngModel)]="email"
                      required
                      placeholder="usuario@biblioteca.com"
                    />
                  </div>
                </div>

                <div class="mb-4">
                  <label class="form-label fw-semibold">Contraseña</label>
                  <div class="input-group">
                    <span class="input-group-text"><i class="bi bi-lock"></i></span>
                    <input
                      type="password"
                      class="form-control"
                      name="password"
                      [(ngModel)]="password"
                      required
                      placeholder="••••••••"
                    />
                  </div>
                </div>

                <button
                  type="submit"
                  class="btn btn-primary w-100 py-2 fw-semibold shadow-sm"
                  [disabled]="isLoading || !email || !password"
                >
                  @if (isLoading) {
                    <span class="spinner-border spinner-border-sm me-2"></span> Entrando...
                  } @else {
                    <i class="bi bi-box-arrow-in-right me-1"></i> Iniciar Sesión
                  }
                </button>
              </form>

              <hr class="my-4">

              <!-- Botones de acceso rápido para demostración -->
              <div class="text-center">
                <p class="small text-muted mb-2 fw-semibold">Accesos Rápidos de Demostración:</p>
                <div class="d-flex flex-wrap justify-content-center gap-2">
                  <button type="button" class="btn btn-outline-danger btn-sm" (click)="fillDemo('admin@biblioteca.com', 'Admin123!')">
                    <i class="bi bi-shield-lock me-1"></i> Admin
                  </button>
                  <button type="button" class="btn btn-outline-success btn-sm" (click)="fillDemo('biblio@biblioteca.com', 'Biblio123!')">
                    <i class="bi bi-person-gear me-1"></i> Bibliotecario
                  </button>
                  <button type="button" class="btn btn-outline-info btn-sm" (click)="fillDemo('lector@biblioteca.com', 'Lector123!')">
                    <i class="bi bi-person me-1"></i> Lector
                  </button>
                </div>
              </div>

              <div class="text-center mt-4">
                <span class="text-muted small">¿No tienes cuenta?</span>
                <a routerLink="/register" class="text-decoration-none fw-semibold ms-1 small">Regístrate como Lector</a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  isLoading = false;
  errorMessage = '';

  fillDemo(e: string, p: string): void {
    this.email = e;
    this.password = p;
    this.errorMessage = '';
  }

  onSubmit(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/libros']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Error al iniciar sesión. Verifique sus credenciales.';
      }
    });
  }
}
