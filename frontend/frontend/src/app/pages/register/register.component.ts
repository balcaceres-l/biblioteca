import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-12 col-md-10 col-lg-7">
          <div class="card shadow-sm border-0 rounded-4">
            <div class="card-body p-4 p-md-5">
              <div class="text-center mb-4">
                <i class="bi bi-person-plus-fill text-primary display-4"></i>
                <h3 class="fw-bold mt-2">Registro de Lector</h3>
                <p class="text-muted">Crea tu cuenta de Lector para solicitar y consultar tus préstamos</p>
              </div>

              @if (errorMessage) {
                <div class="alert alert-danger d-flex align-items-center gap-2" role="alert">
                  <i class="bi bi-exclamation-triangle-fill"></i>
                  <div>{{ errorMessage }}</div>
                </div>
              }

              <form (ngSubmit)="onSubmit()" #registerForm="ngForm">
                <div class="row g-3">
                  <div class="col-12 col-md-6">
                    <label class="form-label fw-semibold">Nombre Completo *</label>
                    <input
                      type="text"
                      class="form-control"
                      name="nombreCompleto"
                      [(ngModel)]="nombreCompleto"
                      required
                      placeholder="Ej. Juan Pérez"
                    />
                  </div>

                  <div class="col-12 col-md-6">
                    <label class="form-label fw-semibold">Documento de Identidad *</label>
                    <input
                      type="text"
                      class="form-control"
                      name="documentoIdentidad"
                      [(ngModel)]="documentoIdentidad"
                      required
                      placeholder="Ej. 01234567-8"
                    />
                  </div>

                  <div class="col-12 col-md-6">
                    <label class="form-label fw-semibold">Correo Electrónico *</label>
                    <input
                      type="email"
                      class="form-control"
                      name="email"
                      [(ngModel)]="email"
                      required
                      placeholder="juan@correo.com"
                    />
                  </div>

                  <div class="col-12 col-md-6">
                    <label class="form-label fw-semibold">Contraseña *</label>
                    <input
                      type="password"
                      class="form-control"
                      name="password"
                      [(ngModel)]="password"
                      required
                      minlength="6"
                      placeholder="Mínimo 6 caracteres"
                    />
                  </div>

                  <div class="col-12 col-md-6">
                    <label class="form-label fw-semibold">Teléfono</label>
                    <input
                      type="text"
                      class="form-control"
                      name="telefono"
                      [(ngModel)]="telefono"
                      placeholder="Ej. 7890-1234"
                    />
                  </div>

                  <div class="col-12 col-md-6">
                    <label class="form-label fw-semibold">Dirección</label>
                    <input
                      type="text"
                      class="form-control"
                      name="direccion"
                      [(ngModel)]="direccion"
                      placeholder="Ej. San Salvador"
                    />
                  </div>
                </div>

                <div class="mt-4">
                  <button
                    type="submit"
                    class="btn btn-primary w-100 py-2 fw-semibold shadow-sm"
                    [disabled]="isLoading || !nombreCompleto || !documentoIdentidad || !email || !password"
                  >
                    @if (isLoading) {
                      <span class="spinner-border spinner-border-sm me-2"></span> Registrando...
                    } @else {
                      <i class="bi bi-check-circle me-1"></i> Completar Registro
                    }
                  </button>
                </div>
              </form>

              <div class="text-center mt-4">
                <span class="text-muted small">¿Ya tienes una cuenta?</span>
                <a routerLink="/login" class="text-decoration-none fw-semibold ms-1 small">Inicia Sesión</a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  nombreCompleto = '';
  documentoIdentidad = '';
  email = '';
  password = '';
  telefono = '';
  direccion = '';

  isLoading = false;
  errorMessage = '';

  onSubmit(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const payload = {
      nombreCompleto: this.nombreCompleto,
      documentoIdentidad: this.documentoIdentidad,
      email: this.email,
      password: this.password,
      telefono: this.telefono || null,
      direccion: this.direccion || null
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/libros']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Error al completar el registro.';
      }
    });
  }
}
