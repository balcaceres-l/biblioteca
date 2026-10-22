import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary shadow-sm">
      <div class="container-fluid">
        <a class="navbar-brand fw-bold d-flex align-items-center gap-2" routerLink="/libros">
          <i class="bi bi-book-half fs-4"></i>
          <span>Biblioteca Central</span>
        </a>

        @if (authService.isLoggedIn()) {
          <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navMenu">
            <span class="navbar-toggler-icon"></span>
          </button>

          <div class="collapse navbar-collapse" id="navMenu">
            <ul class="navbar-nav me-auto mb-2 mb-lg-0">
              <li class="nav-item">
                <a class="nav-link" routerLink="/libros" routerLinkActive="active">
                  <i class="bi bi-journal-richtext me-1"></i> Libros
                </a>
              </li>
              <li class="nav-item">
                <a class="nav-link" routerLink="/autores" routerLinkActive="active">
                  <i class="bi bi-person-lines-fill me-1"></i> Autores
                </a>
              </li>
              <li class="nav-item">
                <a class="nav-link" routerLink="/categorias" routerLinkActive="active">
                  <i class="bi bi-tags me-1"></i> Categorías
                </a>
              </li>

              @if (authService.isAdmin() || authService.isBibliotecario()) {
                <li class="nav-item">
                  <a class="nav-link" routerLink="/lectores" routerLinkActive="active">
                    <i class="bi bi-people me-1"></i> Lectores
                  </a>
                </li>
              }

              <li class="nav-item">
                <a class="nav-link" routerLink="/prestamos" routerLinkActive="active">
                  <i class="bi bi-arrow-left-right me-1"></i>
                  <span>{{ authService.isLector() ? 'Mis Préstamos' : 'Préstamos' }}</span>
                </a>
              </li>
            </ul>

            <div class="d-flex align-items-center gap-3">
              <span class="badge bg-light text-primary fs-6 px-3 py-2 rounded-pill shadow-sm">
                <i class="bi bi-person-badge me-1"></i>
                <strong>{{ authService.currentUser() }}</strong>
                <span class="text-muted ms-1">({{ authService.currentRole() }})</span>
              </span>

              <button class="btn btn-outline-light btn-sm" (click)="logout()">
                <i class="bi bi-box-arrow-right me-1"></i> Salir
              </button>
            </div>
          </div>
        } @else {
          <div class="d-flex gap-2">
            <a class="btn btn-light btn-sm" routerLink="/login">Iniciar Sesión</a>
            <a class="btn btn-outline-light btn-sm" routerLink="/register">Registrarse</a>
          </div>
        }
      </div>
    </nav>
  `
})
export class NavbarComponent {
  public authService = inject(AuthService);
  private router = inject(Router);

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
