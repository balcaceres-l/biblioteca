import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CategoriasService } from '../../core/services/categorias.service';
import { AuthService } from '../../core/services/auth.service';
import { Categoria, Libro, PagedResult } from '../../core/models/biblioteca.models';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-categorias',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="container-fluid py-4 px-md-4">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
        <div>
          <h2 class="fw-bold mb-1"><i class="bi bi-tags-fill text-primary me-2"></i>Gestión de Categorías</h2>
          <p class="text-muted mb-0">Administra las clasificaciones temáticas de los libros en el catálogo</p>
        </div>

        @if (authService.hasPermission('Categorias.Gestionar')) {
          <button class="btn btn-primary d-flex align-items-center gap-2 shadow-sm" (click)="openCreateModal()">
            <i class="bi bi-plus-circle"></i>
            <span>Nueva Categoría</span>
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
                  placeholder="Buscar categoría por nombre o descripción..."
                  [(ngModel)]="searchFilter"
                  (keyup.enter)="loadCategorias()"
                />
              </div>
            </div>
            <div class="col-6 col-md-3">
              <select class="form-select" [(ngModel)]="activaFilter" (change)="loadCategorias()">
                <option [ngValue]="null">Todas</option>
                <option [ngValue]="true">Solo Activas</option>
                <option [ngValue]="false">Inactivas</option>
              </select>
            </div>
            <div class="col-6 col-md-3">
              <button class="btn btn-outline-secondary w-100" (click)="loadCategorias()">
                <i class="bi bi-filter me-1"></i> Filtrar
              </button>
            </div>
          </div>
        </div>
      </div>

      @if (isLoading) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="text-muted mt-2">Cargando categorías...</p>
        </div>
      } @else if (categoriasResult.items.length === 0) {
        <div class="card border-0 shadow-sm text-center py-5 my-3">
          <div class="card-body">
            <i class="bi bi-tags text-muted display-3"></i>
            <h5 class="fw-bold mt-3">No se encontraron categorías</h5>
          </div>
        </div>
      } @else {
        <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead class="table-light">
                <tr>
                  <th scope="col" class="ps-3">Nombre</th>
                  <th scope="col">Descripción</th>
                  <th scope="col">Libros Asociados</th>
                  <th scope="col">Estado</th>
                  <th scope="col" class="text-end pe-3">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (cat of categoriasResult.items; track cat.id) {
                  <tr>
                    <td class="ps-3 fw-bold">{{ cat.nombre }}</td>
                    <td class="text-muted small">{{ cat.descripcion || 'Sin descripción' }}</td>
                    <td>
                      <span class="badge bg-secondary-subtle text-secondary px-2 py-1 rounded-pill">
                        {{ cat.cantidadLibros }} libro(s)
                      </span>
                    </td>
                    <td>
                      @if (authService.hasPermission('Categorias.Gestionar')) {
                        <button
                          class="btn btn-sm badge rounded-pill border-0"
                          [ngClass]="cat.activa ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger'"
                          (click)="toggleEstado(cat)"
                        >
                          <i class="bi" [ngClass]="cat.activa ? 'bi-check-circle' : 'bi-dash-circle'"></i>
                          {{ cat.activa ? 'Activa' : 'Inactiva' }}
                        </button>
                      } @else {
                        <span class="badge rounded-pill" [ngClass]="cat.activa ? 'bg-success' : 'bg-danger'">
                          {{ cat.activa ? 'Activa' : 'Inactiva' }}
                        </span>
                      }
                    </td>
                    <td class="text-end pe-3">
                      <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-info" (click)="openLibrosModal(cat)" title="Ver Libros">
                          <i class="bi bi-journal-text"></i>
                        </button>
                        @if (authService.hasPermission('Categorias.Gestionar')) {
                          <button class="btn btn-outline-primary" (click)="openEditModal(cat)" title="Editar">
                            <i class="bi bi-pencil"></i>
                          </button>
                        }
                        @if (authService.hasPermission('Categorias.Eliminar')) {
                          <button class="btn btn-outline-danger" (click)="deleteCategoria(cat)" title="Eliminar">
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
              [page]="categoriasResult.page"
              [pageSize]="categoriasResult.pageSize"
              [totalCount]="categoriasResult.totalCount"
              [totalPages]="categoriasResult.totalPages"
              [hasPrevious]="categoriasResult.hasPrevious"
              [hasNext]="categoriasResult.hasNext"
              (pageChange)="onPageChange($event)"
              (pageSizeChange)="onPageSizeChange($event)"
            ></app-pagination>
          </div>
        </div>
      }

      <!-- Modal Crear / Editar Categoría -->
      @if (showCategoriaModal) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">
                  <i class="bi" [ngClass]="isEditing ? 'bi-pencil text-warning' : 'bi-plus-circle text-primary'"></i>
                  <span class="ms-2">{{ isEditing ? 'Editar Categoría' : 'Nueva Categoría' }}</span>
                </h5>
                <button type="button" class="btn-close" (click)="showCategoriaModal = false"></button>
              </div>

              <div class="modal-body p-4">
                <form (ngSubmit)="saveCategoria()">
                  <div class="mb-3">
                    <label class="form-label fw-semibold">Nombre de la Categoría *</label>
                    <input type="text" class="form-control" [(ngModel)]="categoriaForm.nombre" name="nombre" required />
                  </div>

                  <div class="mb-3">
                    <label class="form-label fw-semibold">Descripción</label>
                    <textarea class="form-control" rows="3" [(ngModel)]="categoriaForm.descripcion" name="descripcion"></textarea>
                  </div>

                  <div class="mb-3 form-check form-switch">
                    <input class="form-check-input" type="checkbox" id="catActivaCheck" [(ngModel)]="categoriaForm.activa" name="activa">
                    <label class="form-check-label fw-semibold" for="catActivaCheck">Categoría Activa</label>
                  </div>

                  <div class="modal-footer border-0 px-0 pb-0 mt-4">
                    <button type="button" class="btn btn-light" (click)="showCategoriaModal = false">Cancelar</button>
                    <button type="submit" class="btn btn-primary" [disabled]="isSaving || !categoriaForm.nombre">
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

      <!-- Modal Libros de la Categoría -->
      @if (showLibrosModal && selectedCategoria) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">
                  <i class="bi bi-tags text-primary me-2"></i>Libros en: {{ selectedCategoria.nombre }}
                </h5>
                <button type="button" class="btn-close" (click)="showLibrosModal = false"></button>
              </div>

              <div class="modal-body p-4">
                @if (categoriaLibrosList.length === 0) {
                  <p class="text-muted text-center py-4 mb-0">No hay libros clasificados en esta categoría.</p>
                } @else {
                  <div class="list-group list-group-flush">
                    @for (l of categoriaLibrosList; track l.id) {
                      <div class="list-group-item d-flex justify-content-between align-items-center py-3">
                        <div>
                          <h6 class="mb-1 fw-bold">{{ l.titulo }}</h6>
                          <div class="small text-muted"><i class="bi bi-person me-1"></i>{{ l.autorNombre }} | ISBN: {{ l.isbn }}</div>
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
export class CategoriasComponent implements OnInit {
  private categoriasService = inject(CategoriasService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  categoriasResult: PagedResult<Categoria> = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false };
  searchFilter = '';
  activaFilter: boolean | null = null;
  isLoading = false;
  isSaving = false;

  alertMessage = '';
  alertClass = 'alert-info';
  alertIcon = 'bi-info-circle';

  showCategoriaModal = false;
  isEditing = false;
  categoriaForm: any = { id: 0, nombre: '', descripcion: '', activa: true, rowVersion: '' };

  showLibrosModal = false;
  selectedCategoria: Categoria | null = null;
  categoriaLibrosList: Libro[] = [];

  ngOnInit(): void {
    this.loadCategorias();
  }

  loadCategorias(page = 1, pageSize = 10): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.categoriasService.getAll({ search: this.searchFilter, activa: this.activaFilter ?? undefined, page, pageSize }).subscribe({
      next: (res) => {
        this.categoriasResult = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.showAlert('Error al cargar categorías: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        this.cdr.markForCheck();
      }
    });
  }

  onPageChange(page: number): void {
    this.loadCategorias(page, this.categoriasResult.pageSize);
  }

  onPageSizeChange(size: number): void {
    this.loadCategorias(1, size);
  }

  toggleEstado(cat: Categoria): void {
    this.categoriasService.patchEstado(cat.id, !cat.activa).subscribe({
      next: (updated) => {
        cat.activa = updated.activa;
        this.showAlert(`Estado de '${cat.nombre}' cambiado a ${cat.activa ? 'Activa' : 'Inactiva'}.`, 'alert-info', 'bi-info-circle');
      },
      error: (err) => this.showAlert('Error al cambiar estado: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle')
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.categoriaForm = { id: 0, nombre: '', descripcion: '', activa: true, rowVersion: '' };
    this.showCategoriaModal = true;
  }

  openEditModal(cat: Categoria): void {
    this.isEditing = true;
    this.categoriaForm = {
      id: cat.id,
      nombre: cat.nombre,
      descripcion: cat.descripcion ?? '',
      activa: cat.activa,
      rowVersion: cat.rowVersion ?? ''
    };
    this.showCategoriaModal = true;
  }

  saveCategoria(): void {
    this.isSaving = true;
    if (this.isEditing) {
      this.categoriasService.update(this.categoriaForm.id, this.categoriaForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.showCategoriaModal = false;
          this.showAlert('Categoría actualizada exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadCategorias(this.categoriasResult.page, this.categoriasResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          if (err.status === 409) {
            this.showAlert('Conflicto de concurrencia: La categoría fue modificada por otro usuario. Recargando datos...', 'alert-warning', 'bi-exclamation-circle');
            this.showCategoriaModal = false;
            this.loadCategorias(this.categoriasResult.page, this.categoriasResult.pageSize);
          } else {
            this.showAlert('Error al actualizar categoría: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
          }
        }
      });
    } else {
      this.categoriasService.create(this.categoriaForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.showCategoriaModal = false;
          this.showAlert('Categoría creada exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadCategorias(1, this.categoriasResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          this.showAlert('Error al crear categoría: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        }
      });
    }
  }

  deleteCategoria(cat: Categoria): void {
    if (!confirm(`¿Está seguro de eliminar la categoría '${cat.nombre}'?`)) return;
    this.categoriasService.delete(cat.id).subscribe({
      next: () => {
        this.showAlert(`Categoría '${cat.nombre}' eliminada.`, 'alert-success', 'bi-check-circle');
        this.loadCategorias(this.categoriasResult.page, this.categoriasResult.pageSize);
      },
      error: (err) => this.showAlert('No se puede eliminar la categoría: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle')
    });
  }

  openLibrosModal(cat: Categoria): void {
    this.selectedCategoria = cat;
    this.categoriasService.getLibrosByCategoria(cat.id).subscribe({
      next: (libros) => {
        this.categoriaLibrosList = libros;
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
