import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LibrosService } from '../../core/services/libros.service';
import { AutoresService } from '../../core/services/autores.service';
import { CategoriasService } from '../../core/services/categorias.service';
import { LectoresService } from '../../core/services/lectores.service';
import { PrestamosService } from '../../core/services/prestamos.service';
import { AuthService } from '../../core/services/auth.service';
import { Libro, Autor, Categoria, Lector, PagedResult } from '../../core/models/biblioteca.models';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-libros',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="container-fluid py-4 px-md-4">
      <!-- Encabezado y botón de nuevo libro -->
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
        <div>
          <h2 class="fw-bold mb-1"><i class="bi bi-journal-bookmark-fill text-primary me-2"></i>Catálogo de Libros</h2>
          <p class="text-muted mb-0">Explora el catálogo, consulta disponibilidad y solicita o administra préstamos</p>
        </div>

        @if (authService.hasPermission('Libros.Gestionar')) {
          <button class="btn btn-primary d-flex align-items-center gap-2 shadow-sm" (click)="openCreateModal()">
            <i class="bi bi-plus-circle"></i>
            <span>Nuevo Libro</span>
          </button>
        }
      </div>

      <!-- Alertas de éxito, error y concurrencia -->
      @if (alertMessage) {
        <div class="alert alert-dismissible fade show shadow-sm" [ngClass]="alertClass" role="alert">
          <i class="bi" [ngClass]="alertIcon"></i>
          <span class="ms-2">{{ alertMessage }}</span>
          <button type="button" class="btn-close" (click)="alertMessage = ''"></button>
        </div>
      }

      <!-- Filtros de búsqueda -->
      <div class="card border-0 shadow-sm rounded-4 mb-4">
        <div class="card-body p-3">
          <div class="row g-2 align-items-center">
            <div class="col-12 col-md-4">
              <div class="input-group">
                <span class="input-group-text bg-white border-end-0"><i class="bi bi-search text-muted"></i></span>
                <input
                  type="text"
                  class="form-control border-start-0"
                  placeholder="Buscar por título, autor, ISBN..."
                  [(ngModel)]="searchFilter"
                  (keyup.enter)="loadLibros()"
                />
              </div>
            </div>

            <div class="col-6 col-md-3">
              <select class="form-select" [(ngModel)]="autorFilter" (change)="loadLibros()">
                <option [ngValue]="null">Todos los autores</option>
                @for (a of autoresList; track a.id) {
                  <option [ngValue]="a.id">{{ a.nombreCompleto }}</option>
                }
              </select>
            </div>

            <div class="col-6 col-md-3">
              <select class="form-select" [(ngModel)]="categoriaFilter" (change)="loadLibros()">
                <option [ngValue]="null">Todas las categorías</option>
                @for (c of categoriasList; track c.id) {
                  <option [ngValue]="c.id">{{ c.nombre }}</option>
                }
              </select>
            </div>

            <div class="col-6 col-md-2">
              <select class="form-select" [(ngModel)]="disponibleFilter" (change)="loadLibros()">
                <option [ngValue]="null">Disponibilidad</option>
                <option [ngValue]="true">Solo Disponibles</option>
                <option [ngValue]="false">Prestados / No Disp.</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <!-- Spinner de carga -->
      @if (isLoading) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="text-muted mt-2">Cargando catálogo...</p>
        </div>
      } @else if (librosResult.items.length === 0) {
        <div class="card border-0 shadow-sm text-center py-5 my-3">
          <div class="card-body">
            <i class="bi bi-journal-x text-muted display-3"></i>
            <h5 class="fw-bold mt-3">No se encontraron libros</h5>
            <p class="text-muted">Intenta ajustando los términos de búsqueda o filtros.</p>
          </div>
        </div>
      } @else {
        <!-- Grid de tarjetas de libros -->
        <div class="row row-cols-1 row-cols-sm-2 row-cols-md-3 row-cols-lg-4 g-4 mb-4">
          @for (libro of librosResult.items; track libro.id) {
            <div class="col">
              <div class="card h-100 border-0 shadow-sm rounded-4 overflow-hidden position-relative">
                <!-- Portada del libro -->
                <div class="bg-dark text-white text-center position-relative d-flex align-items-center justify-content-center" style="height: 220px;">
                  @if (libro.portadaUrl) {
                    <img [src]="'http://localhost:5000' + libro.portadaUrl" [alt]="libro.titulo" class="w-100 h-100 object-fit-cover" />
                  } @else {
                    <div class="p-3 text-center">
                      <i class="bi bi-book display-4 opacity-50"></i>
                      <div class="small text-muted mt-1">Sin Portada</div>
                    </div>
                  }

                  <!-- Badge de disponibilidad -->
                  <span
                    class="badge position-absolute top-0 end-0 m-2 shadow-sm"
                    [ngClass]="libro.disponible ? 'bg-success' : 'bg-secondary'"
                  >
                    <i class="bi" [ngClass]="libro.disponible ? 'bi-check-circle-fill' : 'bi-clock-history'"></i>
                    {{ libro.disponible ? 'Disponible' : 'Prestado' }}
                  </span>
                </div>

                <div class="card-body d-flex flex-column p-3">
                  <span class="badge bg-primary-subtle text-primary mb-2 align-self-start">{{ libro.categoriaNombre }}</span>
                  <h6 class="card-title fw-bold mb-1 text-truncate" [title]="libro.titulo">{{ libro.titulo }}</h6>
                  <p class="text-muted small mb-1"><i class="bi bi-person me-1"></i>{{ libro.autorNombre }}</p>
                  <p class="text-muted small mb-2"><i class="bi bi-upc-scan me-1"></i>ISBN: {{ libro.isbn }}</p>

                  @if (libro.resumen) {
                    <p class="text-secondary small mb-3 text-truncate-2 flex-grow-1" [title]="libro.resumen">
                      {{ libro.resumen }}
                    </p>
                  } @else {
                    <div class="flex-grow-1"></div>
                  }

                  <hr class="my-2">

                  <div class="d-flex justify-content-between align-items-center gap-1 mt-auto">
                    <!-- Botón de préstamo -->
                    @if (libro.disponible) {
                      <button class="btn btn-outline-success btn-sm flex-grow-1" (click)="openPrestamoModal(libro)">
                        <i class="bi bi-box-arrow-up-right me-1"></i> Prestar
                      </button>
                    } @else {
                      <button class="btn btn-light btn-sm flex-grow-1 text-muted" (click)="checkDisponibilidad(libro)">
                        <i class="bi bi-info-circle me-1"></i> Ocupado
                      </button>
                    }

                    <!-- Opciones de gestión para bibliotecario/administrador -->
                    @if (authService.hasPermission('Libros.Gestionar')) {
                      <button class="btn btn-outline-primary btn-sm" (click)="openEditModal(libro)" title="Editar">
                        <i class="bi bi-pencil"></i>
                      </button>
                      <button class="btn btn-outline-secondary btn-sm" (click)="openPortadaModal(libro)" title="Subir / Cambiar Portada">
                        <i class="bi bi-image"></i>
                      </button>
                    }

                    @if (authService.hasPermission('Libros.Eliminar')) {
                      <button class="btn btn-outline-danger btn-sm" (click)="deleteLibro(libro)" title="Eliminar">
                        <i class="bi bi-trash"></i>
                      </button>
                    }
                  </div>
                </div>
              </div>
            </div>
          }
        </div>

        <!-- Paginación con Bootstrap -->
        <app-pagination
          [page]="librosResult.page"
          [pageSize]="librosResult.pageSize"
          [totalCount]="librosResult.totalCount"
          [totalPages]="librosResult.totalPages"
          [hasPrevious]="librosResult.hasPrevious"
          [hasNext]="librosResult.hasNext"
          (pageChange)="onPageChange($event)"
          (pageSizeChange)="onPageSizeChange($event)"
        ></app-pagination>
      }

      <!-- Modal Crear / Editar Libro -->
      @if (showLibroModal) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">
                  <i class="bi" [ngClass]="isEditing ? 'bi-pencil-square text-warning' : 'bi-plus-circle text-primary'"></i>
                  <span class="ms-2">{{ isEditing ? 'Editar Libro' : 'Nuevo Libro' }}</span>
                </h5>
                <button type="button" class="btn-close" (click)="closeLibroModal()"></button>
              </div>

              <div class="modal-body p-4">
                <form (ngSubmit)="saveLibro()">
                  <div class="row g-3">
                    <div class="col-12 col-md-8">
                      <label class="form-label fw-semibold">Título *</label>
                      <input type="text" class="form-control" [(ngModel)]="libroForm.titulo" name="titulo" required />
                    </div>

                    <div class="col-12 col-md-4">
                      <label class="form-label fw-semibold">ISBN *</label>
                      <input type="text" class="form-control" [(ngModel)]="libroForm.isbn" name="isbn" required />
                    </div>

                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Autor *</label>
                      <select class="form-select" [(ngModel)]="libroForm.autorId" name="autorId" required>
                        <option [ngValue]="null" disabled>Seleccione un autor</option>
                        @for (a of autoresList; track a.id) {
                          <option [ngValue]="a.id">{{ a.nombreCompleto }}</option>
                        }
                      </select>
                    </div>

                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Categoría *</label>
                      <select class="form-select" [(ngModel)]="libroForm.categoriaId" name="categoriaId" required>
                        <option [ngValue]="null" disabled>Seleccione una categoría</option>
                        @for (c of categoriasList; track c.id) {
                          <option [ngValue]="c.id">{{ c.nombre }}</option>
                        }
                      </select>
                    </div>

                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Editorial</label>
                      <input type="text" class="form-control" [(ngModel)]="libroForm.editorial" name="editorial" />
                    </div>

                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Año de Publicación</label>
                      <input type="number" class="form-control" [(ngModel)]="libroForm.anioPublicacion" name="anioPublicacion" min="1000" max="2100" />
                    </div>

                    <div class="col-12">
                      <label class="form-label fw-semibold">Resumen</label>
                      <textarea class="form-control" rows="3" [(ngModel)]="libroForm.resumen" name="resumen"></textarea>
                    </div>

                    <div class="col-12">
                      <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" id="dispCheck" [(ngModel)]="libroForm.disponible" name="disponible">
                        <label class="form-check-label fw-semibold" for="dispCheck">Disponible para préstamo</label>
                      </div>
                    </div>
                  </div>

                  <div class="modal-footer border-0 px-0 pb-0 mt-4">
                    <button type="button" class="btn btn-light" (click)="closeLibroModal()">Cancelar</button>
                    <button type="submit" class="btn btn-primary" [disabled]="isSaving">
                      @if (isSaving) {
                        <span class="spinner-border spinner-border-sm me-1"></span> Guardando...
                      } @else {
                        <i class="bi bi-save me-1"></i> Guardar Libro
                      }
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- Modal Subir Portada -->
      @if (showPortadaModal && selectedLibro) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0">
                <h5 class="modal-title fw-bold"><i class="bi bi-image text-primary me-2"></i>Portada del Libro</h5>
                <button type="button" class="btn-close" (click)="showPortadaModal = false"></button>
              </div>
              <div class="modal-body text-center p-4">
                <h6 class="fw-bold">{{ selectedLibro.titulo }}</h6>
                <div class="my-3">
                  @if (selectedLibro.portadaUrl) {
                    <img [src]="'http://localhost:5000' + selectedLibro.portadaUrl" class="img-thumbnail shadow-sm mb-3" style="max-height: 200px;" />
                    <div>
                      <button class="btn btn-outline-danger btn-sm" (click)="deletePortada()">
                        <i class="bi bi-trash me-1"></i> Eliminar Portada Actual
                      </button>
                    </div>
                  } @else {
                    <p class="text-muted small">Este libro no cuenta con imagen de portada aún.</p>
                  }
                </div>

                <div class="mb-3 text-start">
                  <label class="form-label fw-semibold">Seleccionar nueva imagen (.jpg, .jpeg, .png, .webp, máx 5MB)</label>
                  <input type="file" class="form-control" (change)="onFileSelected($event)" accept=".jpg,.jpeg,.png,.webp" />
                </div>
              </div>
              <div class="modal-footer border-0">
                <button class="btn btn-light" (click)="showPortadaModal = false">Cerrar</button>
                <button class="btn btn-primary" (click)="uploadPortada()" [disabled]="!selectedFile || isSaving">
                  @if (isSaving) {
                    <span class="spinner-border spinner-border-sm me-1"></span> Subiendo...
                  } @else {
                    <i class="bi bi-upload me-1"></i> Subir Imagen
                  }
                </button>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- Modal Solicitar / Registrar Préstamo -->
      @if (showPrestamoModal && selectedLibro) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold"><i class="bi bi-arrow-left-right text-success me-2"></i>Registrar Préstamo</h5>
                <button type="button" class="btn-close" (click)="showPrestamoModal = false"></button>
              </div>
              <div class="modal-body p-4">
                <div class="alert alert-info py-2 small d-flex align-items-center gap-2">
                  <i class="bi bi-info-circle-fill"></i>
                  <div>Libro seleccionado: <strong>{{ selectedLibro.titulo }}</strong></div>
                </div>

                <form (ngSubmit)="confirmarPrestamo()">
                  @if (authService.isAdmin() || authService.isBibliotecario()) {
                    <div class="mb-3">
                      <label class="form-label fw-semibold">Seleccionar Lector *</label>
                      <select class="form-select" [(ngModel)]="prestamoForm.lectorId" name="lectorId" required>
                        <option [ngValue]="null" disabled>Seleccione un lector</option>
                        @for (lec of lectoresList; track lec.id) {
                          <option [ngValue]="lec.id">{{ lec.nombreCompleto }} (Doc: {{ lec.documentoIdentidad }})</option>
                        }
                      </select>
                    </div>
                  } @else {
                    <div class="mb-3">
                      <p class="small text-muted mb-0">El préstamo se registrará a nombre de tu cuenta de Lector.</p>
                    </div>
                  }

                  <div class="mb-3">
                    <label class="form-label fw-semibold">Días de préstamo</label>
                    <input type="number" class="form-control" [(ngModel)]="prestamoForm.diasPrestamo" name="dias" min="1" max="60" />
                  </div>

                  <div class="mb-3">
                    <label class="form-label fw-semibold">Observaciones</label>
                    <textarea class="form-control" rows="2" [(ngModel)]="prestamoForm.observaciones" name="obs" placeholder="Ej. Ejemplar en perfecto estado"></textarea>
                  </div>

                  <div class="modal-footer border-0 px-0 pb-0 mt-4">
                    <button type="button" class="btn btn-light" (click)="showPrestamoModal = false">Cancelar</button>
                    <button type="submit" class="btn btn-success" [disabled]="isSaving">
                      @if (isSaving) {
                        <span class="spinner-border spinner-border-sm me-1"></span> Procesando préstamo...
                      } @else {
                        <i class="bi bi-check2-circle me-1"></i> Confirmar Préstamo
                      }
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class LibrosComponent implements OnInit {
  private librosService = inject(LibrosService);
  private autoresService = inject(AutoresService);
  private categoriasService = inject(CategoriasService);
  private lectoresService = inject(LectoresService);
  private prestamosService = inject(PrestamosService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  librosResult: PagedResult<Libro> = { items: [], page: 1, pageSize: 8, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false };
  autoresList: Autor[] = [];
  categoriasList: Categoria[] = [];
  lectoresList: Lector[] = [];

  searchFilter = '';
  autorFilter: number | null = null;
  categoriaFilter: number | null = null;
  disponibleFilter: boolean | null = null;

  isLoading = false;
  isSaving = false;
  alertMessage = '';
  alertClass = 'alert-info';
  alertIcon = 'bi-info-circle';

  // Modales
  showLibroModal = false;
  isEditing = false;
  selectedLibro: Libro | null = null;

  libroForm: any = {
    id: 0,
    titulo: '',
    isbn: '',
    resumen: '',
    anioPublicacion: new Date().getFullYear(),
    editorial: '',
    disponible: true,
    autorId: null,
    categoriaId: null,
    rowVersion: ''
  };

  showPortadaModal = false;
  selectedFile: File | null = null;

  showPrestamoModal = false;
  prestamoForm = {
    lectorId: null as number | null,
    diasPrestamo: 14,
    observaciones: ''
  };

  ngOnInit(): void {
    this.loadCatalogos();
    this.loadLibros();
  }

  loadCatalogos(): void {
    this.autoresService.getAll({ pageSize: 100 }).subscribe((res) => {
      this.autoresList = res.items;
      this.cdr.markForCheck();
    });
    this.categoriasService.getAll({ pageSize: 100, activa: true }).subscribe((res) => {
      this.categoriasList = res.items;
      this.cdr.markForCheck();
    });
    if (this.authService.isAdmin() || this.authService.isBibliotecario()) {
      this.lectoresService.getAll({ pageSize: 100, activo: true }).subscribe((res) => {
        this.lectoresList = res.items;
        this.cdr.markForCheck();
      });
    }
  }

  loadLibros(page = 1, pageSize = 8): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.librosService.getAll({
      search: this.searchFilter,
      autorId: this.autorFilter ?? undefined,
      categoriaId: this.categoriaFilter ?? undefined,
      disponible: this.disponibleFilter ?? undefined,
      page,
      pageSize
    }).subscribe({
      next: (res) => {
        this.librosResult = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.showAlert('Error al cargar libros: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        this.cdr.markForCheck();
      }
    });
  }

  onPageChange(page: number): void {
    this.loadLibros(page, this.librosResult.pageSize);
  }

  onPageSizeChange(size: number): void {
    this.loadLibros(1, size);
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedLibro = null;
    this.libroForm = {
      id: 0,
      titulo: '',
      isbn: '',
      resumen: '',
      anioPublicacion: new Date().getFullYear(),
      editorial: '',
      disponible: true,
      autorId: this.autoresList[0]?.id ?? null,
      categoriaId: this.categoriasList[0]?.id ?? null,
      rowVersion: ''
    };
    this.showLibroModal = true;
  }

  openEditModal(libro: Libro): void {
    this.isEditing = true;
    this.selectedLibro = libro;
    this.libroForm = {
      id: libro.id,
      titulo: libro.titulo,
      isbn: libro.isbn,
      resumen: libro.resumen ?? '',
      anioPublicacion: libro.anioPublicacion,
      editorial: libro.editorial ?? '',
      disponible: libro.disponible,
      autorId: libro.autorId,
      categoriaId: libro.categoriaId,
      rowVersion: libro.rowVersion ?? ''
    };
    this.showLibroModal = true;
  }

  closeLibroModal(): void {
    this.showLibroModal = false;
  }

  saveLibro(): void {
    this.isSaving = true;
    if (this.isEditing) {
      this.librosService.update(this.libroForm.id, this.libroForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeLibroModal();
          this.showAlert('Libro actualizado exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadLibros(this.librosResult.page, this.librosResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          if (err.status === 409) {
            // Manejo de conflicto de concurrencia optimista
            this.showAlert('Conflicto de concurrencia: El libro fue modificado por otro usuario. Se recargaron los datos más recientes.', 'alert-warning', 'bi-exclamation-circle');
            this.loadLibros(this.librosResult.page, this.librosResult.pageSize);
            this.closeLibroModal();
          } else {
            this.showAlert('Error al actualizar libro: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
          }
        }
      });
    } else {
      this.librosService.create(this.libroForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.closeLibroModal();
          this.showAlert('Libro creado exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadLibros(1, this.librosResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          this.showAlert('Error al crear libro: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        }
      });
    }
  }

  openPortadaModal(libro: Libro): void {
    this.selectedLibro = libro;
    this.selectedFile = null;
    this.showPortadaModal = true;
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) this.selectedFile = file;
  }

  uploadPortada(): void {
    if (!this.selectedLibro || !this.selectedFile) return;
    this.isSaving = true;
    this.librosService.uploadPortada(this.selectedLibro.id, this.selectedFile).subscribe({
      next: (updated) => {
        this.isSaving = false;
        this.selectedLibro!.portadaUrl = updated.portadaUrl;
        this.showPortadaModal = false;
        this.showAlert('Portada actualizada exitosamente.', 'alert-success', 'bi-check-circle');
        this.loadLibros(this.librosResult.page, this.librosResult.pageSize);
      },
      error: (err) => {
        this.isSaving = false;
        this.showAlert('Error al subir portada: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
      }
    });
  }

  deletePortada(): void {
    if (!this.selectedLibro) return;
    this.librosService.deletePortada(this.selectedLibro.id).subscribe({
      next: () => {
        this.selectedLibro!.portadaUrl = null;
        this.showPortadaModal = false;
        this.showAlert('Portada eliminada.', 'alert-info', 'bi-info-circle');
        this.loadLibros(this.librosResult.page, this.librosResult.pageSize);
      },
      error: (err) => this.showAlert('Error al eliminar portada: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle')
    });
  }

  deleteLibro(libro: Libro): void {
    if (!confirm(`¿Está seguro de eliminar el libro '${libro.titulo}'?`)) return;
    this.librosService.delete(libro.id).subscribe({
      next: () => {
        this.showAlert(`Libro '${libro.titulo}' eliminado exitosamente.`, 'alert-success', 'bi-check-circle');
        this.loadLibros(this.librosResult.page, this.librosResult.pageSize);
      },
      error: (err) => {
        this.showAlert('No se pudo eliminar el libro: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
      }
    });
  }

  openPrestamoModal(libro: Libro): void {
    this.selectedLibro = libro;
    this.prestamoForm = {
      lectorId: this.lectoresList[0]?.id ?? null,
      diasPrestamo: 14,
      observaciones: ''
    };
    this.showPrestamoModal = true;
  }

  confirmarPrestamo(): void {
    if (!this.selectedLibro) return;
    this.isSaving = true;

    this.prestamosService.registrarPrestamo({
      libroId: this.selectedLibro.id,
      lectorId: this.prestamoForm.lectorId ?? undefined,
      diasPrestamo: this.prestamoForm.diasPrestamo,
      observaciones: this.prestamoForm.observaciones,
      libroRowVersion: this.selectedLibro.rowVersion ?? undefined
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.showPrestamoModal = false;
        this.showAlert(`¡Préstamo registrado con éxito para '${this.selectedLibro?.titulo}'!`, 'alert-success', 'bi-check2-circle');
        this.loadLibros(this.librosResult.page, this.librosResult.pageSize);
      },
      error: (err) => {
        this.isSaving = false;
        if (err.status === 409) {
          // Conflicto de concurrencia: el libro acaba de ser prestado por otro usuario
          this.showAlert('Conflicto de concurrencia: El libro ya no se encuentra disponible porque fue prestado o modificado simultáneamente por otro usuario.', 'alert-warning', 'bi-exclamation-octagon');
          this.showPrestamoModal = false;
          this.loadLibros(this.librosResult.page, this.librosResult.pageSize);
        } else {
          this.showAlert('Error al registrar préstamo: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        }
      }
    });
  }

  checkDisponibilidad(libro: Libro): void {
    this.librosService.getDisponibilidad(libro.id).subscribe({
      next: (info) => {
        if (!info.disponible) {
          const lectorMsg = info.lectorPrestamoNombre ? ` por ${info.lectorPrestamoNombre}` : '';
          const fechaMsg = info.fechaVencimiento ? ` hasta el ${new Date(info.fechaVencimiento).toLocaleDateString()}` : '';
          this.showAlert(`El libro '${info.titulo}' está actualmente prestado${lectorMsg}${fechaMsg}.`, 'alert-info', 'bi-clock-history');
        } else {
          this.showAlert(`El libro '${info.titulo}' se encuentra disponible.`, 'alert-success', 'bi-check-circle');
        }
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
