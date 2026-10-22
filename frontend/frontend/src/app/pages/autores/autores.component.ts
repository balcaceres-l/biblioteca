import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AutoresService } from '../../core/services/autores.service';
import { AuthService } from '../../core/services/auth.service';
import { Autor, Libro, PagedResult } from '../../core/models/biblioteca.models';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-autores',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="container-fluid py-4 px-md-4">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
        <div>
          <h2 class="fw-bold mb-1"><i class="bi bi-person-lines-fill text-primary me-2"></i>Gestión de Autores</h2>
          <p class="text-muted mb-0">Administra los autores del catálogo y consulta sus obras publicadas</p>
        </div>

        @if (authService.hasPermission('Autores.Gestionar')) {
          <button class="btn btn-primary d-flex align-items-center gap-2 shadow-sm" (click)="openCreateModal()">
            <i class="bi bi-person-plus-fill"></i>
            <span>Nuevo Autor</span>
          </button>
        }
      </div>

      @if (alertMessage) {
        <div class="alert alert-dismissible fade show shadow-sm" [ngClass]="alertClass" role="alert">
          <i class="bi" [ngClass]="alertIcon"></i>
          <span class="ms-2">{{ alertMessage }}</span>
          <button type="button" class="btn-close" (click)="alertMessage = ''"></button>
        </div>
      }

      <div class="card border-0 shadow-sm rounded-4 mb-4">
        <div class="card-body p-3">
          <div class="row g-2 align-items-center">
            <div class="col-12 col-md-6">
              <div class="input-group">
                <span class="input-group-text bg-white border-end-0"><i class="bi bi-search text-muted"></i></span>
                <input
                  type="text"
                  class="form-control border-start-0"
                  placeholder="Buscar autor por nombre, apellido o nacionalidad..."
                  [(ngModel)]="searchFilter"
                  (keyup.enter)="loadAutores()"
                />
              </div>
            </div>
            <div class="col-12 col-md-2">
              <button class="btn btn-outline-secondary w-100" (click)="loadAutores()">
                <i class="bi bi-filter me-1"></i> Filtrar
              </button>
            </div>
          </div>
        </div>
      </div>

      @if (isLoading) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="text-muted mt-2">Cargando autores...</p>
        </div>
      } @else if (autoresResult.items.length === 0) {
        <div class="card border-0 shadow-sm text-center py-5 my-3">
          <div class="card-body">
            <i class="bi bi-person-x text-muted display-3"></i>
            <h5 class="fw-bold mt-3">No se encontraron autores</h5>
          </div>
        </div>
      } @else {
        <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead class="table-light">
                <tr>
                  <th scope="col" class="ps-3">Nombre Completo</th>
                  <th scope="col">Nacionalidad</th>
                  <th scope="col">Nacimiento</th>
                  <th scope="col">Libros Registrados</th>
                  <th scope="col" class="text-end pe-3">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (autor of autoresResult.items; track autor.id) {
                  <tr>
                    <td class="ps-3 fw-semibold">
                      <i class="bi bi-person-circle text-muted me-2"></i>{{ autor.nombreCompleto }}
                    </td>
                    <td>{{ autor.nacionalidad || 'N/D' }}</td>
                    <td>{{ autor.fechaNacimiento ? (autor.fechaNacimiento | date:'yyyy-MM-dd') : 'N/D' }}</td>
                    <td>
                      <span class="badge bg-secondary-subtle text-secondary px-2 py-1 rounded-pill">
                        {{ autor.cantidadLibros }} obra(s)
                      </span>
                    </td>
                    <td class="text-end pe-3">
                      <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-info" (click)="openLibrosModal(autor)" title="Ver Libros">
                          <i class="bi bi-journal-album"></i>
                        </button>
                        @if (authService.hasPermission('Autores.Gestionar')) {
                          <button class="btn btn-outline-primary" (click)="openEditModal(autor)" title="Editar">
                            <i class="bi bi-pencil"></i>
                          </button>
                        }
                        @if (authService.hasPermission('Autores.Eliminar')) {
                          <button class="btn btn-outline-danger" (click)="deleteAutor(autor)" title="Eliminar">
                            <i class="bi bi-trash"></i>
                          </button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="card-footer bg-white border-0 px-3">
            <app-pagination
              [page]="autoresResult.page"
              [pageSize]="autoresResult.pageSize"
              [totalCount]="autoresResult.totalCount"
              [totalPages]="autoresResult.totalPages"
              [hasPrevious]="autoresResult.hasPrevious"
              [hasNext]="autoresResult.hasNext"
              (pageChange)="onPageChange($event)"
              (pageSizeChange)="onPageSizeChange($event)"
            ></app-pagination>
          </div>
        </div>
      }

      <!-- Modal Crear / Editar Autor -->
      @if (showAutorModal) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">
                  <i class="bi" [ngClass]="isEditing ? 'bi-pencil text-warning' : 'bi-plus-circle text-primary'"></i>
                  <span class="ms-2">{{ isEditing ? 'Editar Autor' : 'Nuevo Autor' }}</span>
                </h5>
                <button type="button" class="btn-close" (click)="showAutorModal = false"></button>
              </div>

              <div class="modal-body p-4">
                <form (ngSubmit)="saveAutor()">
                  <div class="mb-3">
                    <label class="form-label fw-semibold">Nombre *</label>
                    <input type="text" class="form-control" [(ngModel)]="autorForm.nombre" name="nombre" required />
                  </div>

                  <div class="mb-3">
                    <label class="form-label fw-semibold">Apellido *</label>
                    <input type="text" class="form-control" [(ngModel)]="autorForm.apellido" name="apellido" required />
                  </div>

                  <div class="mb-3">
                    <label class="form-label fw-semibold">Nacionalidad</label>
                    <input type="text" class="form-control" [(ngModel)]="autorForm.nacionalidad" name="nacionalidad" />
                  </div>

                  <div class="mb-3">
                    <label class="form-label fw-semibold">Fecha de Nacimiento</label>
                    <input type="date" class="form-control" [(ngModel)]="autorForm.fechaNacimiento" name="fechaNacimiento" />
                  </div>

                  <div class="mb-3">
                    <label class="form-label fw-semibold">Biografía</label>
                    <textarea class="form-control" rows="3" [(ngModel)]="autorForm.biografia" name="biografia"></textarea>
                  </div>

                  <div class="modal-footer border-0 px-0 pb-0 mt-4">
                    <button type="button" class="btn btn-light" (click)="showAutorModal = false">Cancelar</button>
                    <button type="submit" class="btn btn-primary" [disabled]="isSaving || !autorForm.nombre || !autorForm.apellido">
                      @if (isSaving) {
                        <span class="spinner-border spinner-border-sm me-1"></span> Guardando...
                      } @else {
                        <i class="bi bi-save me-1"></i> Guardar
                      }
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- Modal Libros del Autor -->
      @if (showLibrosModal && selectedAutor) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">
                  <i class="bi bi-journals text-primary me-2"></i>Libros de {{ selectedAutor.nombreCompleto }}
                </h5>
                <button type="button" class="btn-close" (click)="showLibrosModal = false"></button>
              </div>

              <div class="modal-body p-4">
                @if (autorLibrosList.length === 0) {
                  <p class="text-muted text-center py-4 mb-0">Este autor no tiene libros registrados en el catálogo.</p>
                } @else {
                  <div class="list-group list-group-flush">
                    @for (l of autorLibrosList; track l.id) {
                      <div class="list-group-item d-flex justify-content-between align-items-center py-3">
                        <div>
                          <h6 class="mb-1 fw-bold">{{ l.titulo }}</h6>
                          <div class="small text-muted">ISBN: {{ l.isbn }} | Categoría: {{ l.categoriaNombre }} ({{ l.anioPublicacion }})</div>
                        </div>
                        <span class="badge" [ngClass]="l.disponible ? 'bg-success' : 'bg-secondary'">
                          {{ l.disponible ? 'Disponible' : 'Prestado' }}
                        </span>
                      </div>
                    }
                  </div>
                }
              </div>

              <div class="modal-footer border-0">
                <button class="btn btn-light" (click)="showLibrosModal = false">Cerrar</button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class AutoresComponent implements OnInit {
  private autoresService = inject(AutoresService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  autoresResult: PagedResult<Autor> = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false };
  searchFilter = '';
  isLoading = false;
  isSaving = false;

  alertMessage = '';
  alertClass = 'alert-info';
  alertIcon = 'bi-info-circle';

  showAutorModal = false;
  isEditing = false;
  autorForm: any = { id: 0, nombre: '', apellido: '', nacionalidad: '', biografia: '', fechaNacimiento: null, rowVersion: '' };

  showLibrosModal = false;
  selectedAutor: Autor | null = null;
  autorLibrosList: Libro[] = [];

  ngOnInit(): void {
    this.loadAutores();
  }

  loadAutores(page = 1, pageSize = 10): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.autoresService.getAll({ search: this.searchFilter, page, pageSize }).subscribe({
      next: (res) => {
        this.autoresResult = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.showAlert('Error al cargar autores: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        this.cdr.markForCheck();
      }
    });
  }

  onPageChange(page: number): void {
    this.loadAutores(page, this.autoresResult.pageSize);
  }

  onPageSizeChange(size: number): void {
    this.loadAutores(1, size);
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.autorForm = { id: 0, nombre: '', apellido: '', nacionalidad: '', biografia: '', fechaNacimiento: null, rowVersion: '' };
    this.showAutorModal = true;
  }

  openEditModal(autor: Autor): void {
    this.isEditing = true;
    this.autorForm = {
      id: autor.id,
      nombre: autor.nombre,
      apellido: autor.apellido,
      nacionalidad: autor.nacionalidad ?? '',
      biografia: autor.biografia ?? '',
      fechaNacimiento: autor.fechaNacimiento ? autor.fechaNacimiento.substring(0, 10) : null,
      rowVersion: autor.rowVersion ?? ''
    };
    this.showAutorModal = true;
  }

  saveAutor(): void {
    this.isSaving = true;
    if (this.isEditing) {
      this.autoresService.update(this.autorForm.id, this.autorForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.showAutorModal = false;
          this.showAlert('Autor actualizado exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadAutores(this.autoresResult.page, this.autoresResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          if (err.status === 409) {
            this.showAlert('Conflicto de concurrencia: El autor fue modificado por otro usuario. Recargando datos...', 'alert-warning', 'bi-exclamation-circle');
            this.showAutorModal = false;
            this.loadAutores(this.autoresResult.page, this.autoresResult.pageSize);
          } else {
            this.showAlert('Error al actualizar autor: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
          }
        }
      });
    } else {
      this.autoresService.create(this.autorForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.showAutorModal = false;
          this.showAlert('Autor creado exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadAutores(1, this.autoresResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          this.showAlert('Error al crear autor: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        }
      });
    }
  }

  deleteAutor(autor: Autor): void {
    if (!confirm(`¿Está seguro de eliminar al autor '${autor.nombreCompleto}'?`)) return;
    this.autoresService.delete(autor.id).subscribe({
      next: () => {
        this.showAlert(`Autor '${autor.nombreCompleto}' eliminado.`, 'alert-success', 'bi-check-circle');
        this.loadAutores(this.autoresResult.page, this.autoresResult.pageSize);
      },
      error: (err) => {
        this.showAlert('No se puede eliminar el autor: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
      }
    });
  }

  openLibrosModal(autor: Autor): void {
    this.selectedAutor = autor;
    this.autoresService.getLibrosByAutor(autor.id).subscribe({
      next: (libros) => {
        this.autorLibrosList = libros;
        this.showLibrosModal = true;
        this.cdr.markForCheck();
      }
    });
  }

  private showAlert(msg: string, cssClass: string, icon: string): void {
    this.alertMessage = msg;
    this.alertClass = cssClass;
    this.alertIcon = icon;
    this.cdr.markForCheck();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
