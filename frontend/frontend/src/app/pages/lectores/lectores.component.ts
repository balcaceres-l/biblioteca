import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LectoresService } from '../../core/services/lectores.service';
import { AuthService } from '../../core/services/auth.service';
import { Lector, Prestamo, PagedResult } from '../../core/models/biblioteca.models';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-lectores',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="container-fluid py-4 px-md-4">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
        <div>
          <h2 class="fw-bold mb-1"><i class="bi bi-people-fill text-primary me-2"></i>Gestión de Lectores</h2>
          <p class="text-muted mb-0">Administra el padrón de lectores autorizados y consulta sus préstamos</p>
        </div>

        @if (authService.hasPermission('Lectores.Gestionar')) {
          <button class="btn btn-primary d-flex align-items-center gap-2 shadow-sm" (click)="openCreateModal()">
            <i class="bi bi-person-plus-fill"></i>
            <span>Nuevo Lector</span>
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
                  placeholder="Buscar lector por nombre, documento, correo o teléfono..."
                  [(ngModel)]="searchFilter"
                  (keyup.enter)="loadLectores()"
                />
              </div>
            </div>
            <div class="col-6 col-md-3">
              <select class="form-select" [(ngModel)]="activoFilter" (change)="loadLectores()">
                <option [ngValue]="null">Todos los estados</option>
                <option [ngValue]="true">Solo Activos</option>
                <option [ngValue]="false">Inactivos</option>
              </select>
            </div>
            <div class="col-6 col-md-3">
              <button class="btn btn-outline-secondary w-100" (click)="loadLectores()">
                <i class="bi bi-filter me-1"></i> Filtrar
              </button>
            </div>
          </div>
        </div>
      </div>

      @if (isLoading) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="text-muted mt-2">Cargando lectores...</p>
        </div>
      } @else if (lectoresResult.items.length === 0) {
        <div class="card border-0 shadow-sm text-center py-5 my-3">
          <div class="card-body">
            <i class="bi bi-person-x text-muted display-3"></i>
            <h5 class="fw-bold mt-3">No se encontraron lectores</h5>
          </div>
        </div>
      } @else {
        <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead class="table-light">
                <tr>
                  <th scope="col" class="ps-3">Nombre Completo</th>
                  <th scope="col">Documento</th>
                  <th scope="col">Correo Electrónico</th>
                  <th scope="col">Teléfono</th>
                  <th scope="col">Préstamos Activos</th>
                  <th scope="col">Estado</th>
                  <th scope="col" class="text-end pe-3">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (lector of lectoresResult.items; track lector.id) {
                  <tr>
                    <td class="ps-3 fw-bold">{{ lector.nombreCompleto }}</td>
                    <td><code>{{ lector.documentoIdentidad }}</code></td>
                    <td>{{ lector.email }}</td>
                    <td>{{ lector.telefono || 'N/D' }}</td>
                    <td>
                      <span class="badge px-2 py-1 rounded-pill" [ngClass]="lector.prestamosActivosCount > 0 ? 'bg-warning text-dark' : 'bg-light text-muted'">
                        {{ lector.prestamosActivosCount }} activo(s)
                      </span>
                    </td>
                    <td>
                      @if (authService.hasPermission('Lectores.Gestionar')) {
                        <button
                          class="btn btn-sm badge rounded-pill border-0"
                          [ngClass]="lector.activo ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger'"
                          (click)="toggleEstado(lector)"
                        >
                          {{ lector.activo ? 'Activo' : 'Inactivo' }}
                        </button>
                      } @else {
                        <span class="badge rounded-pill" [ngClass]="lector.activo ? 'bg-success' : 'bg-danger'">
                          {{ lector.activo ? 'Activo' : 'Inactivo' }}
                        </span>
                      }
                    </td>
                    <td class="text-end pe-3">
                      <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-info" (click)="openPrestamosModal(lector)" title="Ver Historial de Préstamos">
                          <i class="bi bi-clock-history"></i>
                        </button>
                        @if (authService.hasPermission('Lectores.Gestionar')) {
                          <button class="btn btn-outline-primary" (click)="openEditModal(lector)" title="Editar">
                            <i class="bi bi-pencil"></i>
                          </button>
                        }
                        @if (authService.hasPermission('Lectores.Eliminar')) {
                          <button class="btn btn-outline-danger" (click)="deleteLector(lector)" title="Eliminar">
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
              [page]="lectoresResult.page"
              [pageSize]="lectoresResult.pageSize"
              [totalCount]="lectoresResult.totalCount"
              [totalPages]="lectoresResult.totalPages"
              [hasPrevious]="lectoresResult.hasPrevious"
              [hasNext]="lectoresResult.hasNext"
              (pageChange)="onPageChange($event)"
              (pageSizeChange)="onPageSizeChange($event)"
            ></app-pagination>
          </div>
        </div>
      }

      <!-- Modal Crear / Editar Lector -->
      @if (showLectorModal) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">
                  <i class="bi" [ngClass]="isEditing ? 'bi-pencil text-warning' : 'bi-person-plus text-primary'"></i>
                  <span class="ms-2">{{ isEditing ? 'Editar Lector' : 'Nuevo Lector' }}</span>
                </h5>
                <button type="button" class="btn-close" (click)="showLectorModal = false"></button>
              </div>

              <div class="modal-body p-4">
                <form (ngSubmit)="saveLector()">
                  <div class="row g-3">
                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Nombre Completo *</label>
                      <input type="text" class="form-control" [(ngModel)]="lectorForm.nombreCompleto" name="nombreCompleto" required />
                    </div>

                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Documento de Identidad *</label>
                      <input type="text" class="form-control" [(ngModel)]="lectorForm.documentoIdentidad" name="documentoIdentidad" required />
                    </div>

                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Correo Electrónico *</label>
                      <input type="email" class="form-control" [(ngModel)]="lectorForm.email" name="email" required />
                    </div>

                    <div class="col-12 col-md-6">
                      <label class="form-label fw-semibold">Teléfono</label>
                      <input type="text" class="form-control" [(ngModel)]="lectorForm.telefono" name="telefono" />
                    </div>

                    <div class="col-12">
                      <label class="form-label fw-semibold">Dirección</label>
                      <input type="text" class="form-control" [(ngModel)]="lectorForm.direccion" name="direccion" />
                    </div>

                    <div class="col-12">
                      <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" id="lectorActivoCheck" [(ngModel)]="lectorForm.activo" name="activo">
                        <label class="form-check-label fw-semibold" for="lectorActivoCheck">Lector Activo</label>
                      </div>
                    </div>
                  </div>

                  <div class="modal-footer border-0 px-0 pb-0 mt-4">
                    <button type="button" class="btn btn-light" (click)="showLectorModal = false">Cancelar</button>
                    <button type="submit" class="btn btn-primary" [disabled]="isSaving || !lectorForm.nombreCompleto || !lectorForm.documentoIdentidad || !lectorForm.email">
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

      <!-- Modal Historial de Préstamos del Lector -->
      @if (showPrestamosModal && selectedLector) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold">
                  <i class="bi bi-clock-history text-primary me-2"></i>Historial de: {{ selectedLector.nombreCompleto }}
                </h5>
                <button type="button" class="btn-close" (click)="showPrestamosModal = false"></button>
              </div>

              <div class="modal-body p-4">
                @if (lectorPrestamosList.length === 0) {
                  <p class="text-muted text-center py-4 mb-0">El lector no tiene registros de préstamos.</p>
                } @else {
                  <div class="table-responsive">
                    <table class="table table-sm align-middle mb-0">
                      <thead>
                        <tr>
                          <th>Libro</th>
                          <th>Fecha Préstamo</th>
                          <th>Vencimiento</th>
                          <th>Devolución</th>
                          <th>Estado</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (p of lectorPrestamosList; track p.id) {
                          <tr>
                            <td class="fw-semibold">{{ p.libroTitulo }}</td>
                            <td>{{ p.fechaPrestamo | date:'yyyy-MM-dd' }}</td>
                            <td [class.text-danger]="p.estaVencido">{{ p.fechaVencimiento | date:'yyyy-MM-dd' }}</td>
                            <td>{{ p.fechaDevolucionReal ? (p.fechaDevolucionReal | date:'yyyy-MM-dd') : '-' }}</td>
                            <td>
                              <span class="badge" [ngClass]="{
                                'bg-warning text-dark': p.estado === 'Activo' && !p.estaVencido,
                                'bg-danger': p.estaVencido,
                                'bg-success': p.estado === 'Devuelto',
                                'bg-secondary': p.estado === 'Cancelado'
                              }">
                                {{ p.estaVencido ? 'Vencido' : p.estado }}
                              </span>
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                }
              </div>

              <div class="modal-footer border-0">
                <button class="btn btn-light" (click)="showPrestamosModal = false">Cerrar</button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class LectoresComponent implements OnInit {
  private lectoresService = inject(LectoresService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  lectoresResult: PagedResult<Lector> = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false };
  searchFilter = '';
  activoFilter: boolean | null = null;
  isLoading = false;
  isSaving = false;

  alertMessage = '';
  alertClass = 'alert-info';
  alertIcon = 'bi-info-circle';

  showLectorModal = false;
  isEditing = false;
  lectorForm: any = { id: 0, nombreCompleto: '', documentoIdentidad: '', email: '', telefono: '', direccion: '', activo: true, rowVersion: '' };

  showPrestamosModal = false;
  selectedLector: Lector | null = null;
  lectorPrestamosList: Prestamo[] = [];

  ngOnInit(): void {
    this.loadLectores();
  }

  loadLectores(page = 1, pageSize = 10): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.lectoresService.getAll({ search: this.searchFilter, activo: this.activoFilter ?? undefined, page, pageSize }).subscribe({
      next: (res) => {
        this.lectoresResult = res;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.showAlert('Error al cargar lectores: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        this.cdr.markForCheck();
      }
    });
  }

  onPageChange(page: number): void {
    this.loadLectores(page, this.lectoresResult.pageSize);
  }

  onPageSizeChange(size: number): void {
    this.loadLectores(1, size);
  }

  toggleEstado(lector: Lector): void {
    this.lectoresService.patchEstado(lector.id, !lector.activo).subscribe({
      next: (updated) => {
        lector.activo = updated.activo;
        this.showAlert(`Estado de '${lector.nombreCompleto}' actualizado a ${lector.activo ? 'Activo' : 'Inactivo'}.`, 'alert-info', 'bi-info-circle');
      },
      error: (err) => this.showAlert('No se pudo cambiar el estado: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle')
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.lectorForm = { id: 0, nombreCompleto: '', documentoIdentidad: '', email: '', telefono: '', direccion: '', activo: true, rowVersion: '' };
    this.showLectorModal = true;
  }

  openEditModal(lector: Lector): void {
    this.isEditing = true;
    this.lectorForm = {
      id: lector.id,
      nombreCompleto: lector.nombreCompleto,
      documentoIdentidad: lector.documentoIdentidad,
      email: lector.email,
      telefono: lector.telefono ?? '',
      direccion: lector.direccion ?? '',
      activo: lector.activo,
      rowVersion: lector.rowVersion ?? ''
    };
    this.showLectorModal = true;
  }

  saveLector(): void {
    this.isSaving = true;
    if (this.isEditing) {
      this.lectoresService.update(this.lectorForm.id, this.lectorForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.showLectorModal = false;
          this.showAlert('Lector actualizado exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadLectores(this.lectoresResult.page, this.lectoresResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          if (err.status === 409) {
            this.showAlert('Conflicto de concurrencia: El lector fue modificado por otro usuario. Recargando...', 'alert-warning', 'bi-exclamation-circle');
            this.showLectorModal = false;
            this.loadLectores(this.lectoresResult.page, this.lectoresResult.pageSize);
          } else {
            this.showAlert('Error al actualizar lector: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
          }
        }
      });
    } else {
      this.lectoresService.create(this.lectorForm).subscribe({
        next: () => {
          this.isSaving = false;
          this.showLectorModal = false;
          this.showAlert('Lector creado exitosamente.', 'alert-success', 'bi-check-circle');
          this.loadLectores(1, this.lectoresResult.pageSize);
        },
        error: (err) => {
          this.isSaving = false;
          this.showAlert('Error al crear lector: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
        }
      });
    }
  }

  deleteLector(lector: Lector): void {
    if (!confirm(`¿Está seguro de eliminar al lector '${lector.nombreCompleto}'?`)) return;
    this.lectoresService.delete(lector.id).subscribe({
      next: () => {
        this.showAlert(`Lector '${lector.nombreCompleto}' eliminado.`, 'alert-success', 'bi-check-circle');
        this.loadLectores(this.lectoresResult.page, this.lectoresResult.pageSize);
      },
      error: (err) => this.showAlert('No se pudo eliminar el lector: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle')
    });
  }

  openPrestamosModal(lector: Lector): void {
    this.selectedLector = lector;
    this.lectoresService.getHistorialPrestamos(lector.id).subscribe({
      next: (prestamos) => {
        this.lectorPrestamosList = prestamos;
        this.showPrestamosModal = true;
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
