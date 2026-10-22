import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PrestamosService } from '../../core/services/prestamos.service';
import { AuthService } from '../../core/services/auth.service';
import { Prestamo, PagedResult } from '../../core/models/biblioteca.models';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-prestamos',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  template: `
    <div class="container-fluid py-4 px-md-4">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
        <div>
          <h2 class="fw-bold mb-1">
            <i class="bi bi-arrow-left-right text-primary me-2"></i>
            <span>{{ authService.isLector() ? 'Mis Préstamos' : 'Gestión de Préstamos' }}</span>
          </h2>
          <p class="text-muted mb-0">Control de préstamos activos, devoluciones, vencimientos e historial</p>
        </div>
      </div>

      @if (alertMessage) {
        <div class="alert alert-dismissible fade show shadow-sm" [ngClass]="alertClass" role="alert">
          <i class="bi" [ngClass]="alertIcon"></i>
          <span class="ms-2">{{ alertMessage }}</span>
          <button type="button" class="btn-close" (click)="alertMessage = ''"></button>
        </div>
      }

      <!-- Pestañas de filtrado -->
      <ul class="nav nav-pills mb-4 bg-white p-2 rounded-4 shadow-sm">
        <li class="nav-item">
          <button class="nav-link" [class.active]="currentTab === 'todos'" (click)="setTab('todos')">
            <i class="bi bi-list-ul me-1"></i> Todos
          </button>
        </li>
        <li class="nav-item">
          <button class="nav-link" [class.active]="currentTab === 'activos'" (click)="setTab('activos')">
            <i class="bi bi-clock-history me-1"></i> Activos
          </button>
        </li>
        <li class="nav-item">
          <button class="nav-link text-danger" [class.active]="currentTab === 'vencidos'" (click)="setTab('vencidos')">
            <i class="bi bi-exclamation-triangle-fill me-1"></i> Vencidos
          </button>
        </li>
      </ul>

      @if (isLoading) {
        <div class="text-center py-5">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="text-muted mt-2">Cargando préstamos...</p>
        </div>
      } @else if (prestamosResult.items.length === 0) {
        <div class="card border-0 shadow-sm text-center py-5 my-3">
          <div class="card-body">
            <i class="bi bi-inbox text-muted display-3"></i>
            <h5 class="fw-bold mt-3">No hay registros de préstamos</h5>
            <p class="text-muted">No se encontraron préstamos en este filtro.</p>
          </div>
        </div>
      } @else {
        <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead class="table-light">
                <tr>
                  <th scope="col" class="ps-3">Libro</th>
                  @if (!authService.isLector()) {
                    <th scope="col">Lector</th>
                  }
                  <th scope="col">Fecha Préstamo</th>
                  <th scope="col">Vencimiento</th>
                  <th scope="col">Devolución Real</th>
                  <th scope="col">Estado</th>
                  <th scope="col" class="text-end pe-3">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (p of prestamosResult.items; track p.id) {
                  <tr>
                    <td class="ps-3">
                      <div class="d-flex align-items-center gap-2">
                        @if (p.libroPortadaUrl) {
                          <img [src]="'http://localhost:5000' + p.libroPortadaUrl" class="rounded shadow-sm" style="width: 35px; height: 50px; object-fit: cover;" />
                        } @else {
                          <div class="bg-secondary-subtle rounded d-flex align-items-center justify-content-center" style="width: 35px; height: 50px;">
                            <i class="bi bi-book text-muted"></i>
                          </div>
                        }
                        <div>
                          <div class="fw-bold text-truncate" style="max-width: 250px;">{{ p.libroTitulo }}</div>
                          <div class="small text-muted">ISBN: {{ p.libroISBN }}</div>
                        </div>
                      </div>
                    </td>

                    @if (!authService.isLector()) {
                      <td>
                        <div class="fw-semibold">{{ p.lectorNombreCompleto }}</div>
                        <div class="small text-muted">Doc: {{ p.lectorDocumento }}</div>
                      </td>
                    }

                    <td>{{ p.fechaPrestamo | date:'yyyy-MM-dd' }}</td>
                    <td [class.text-danger]="p.estaVencido" [class.fw-bold]="p.estaVencido">
                      {{ p.fechaVencimiento | date:'yyyy-MM-dd' }}
                    </td>
                    <td>{{ p.fechaDevolucionReal ? (p.fechaDevolucionReal | date:'yyyy-MM-dd') : '-' }}</td>
                    <td>
                      <span class="badge rounded-pill" [ngClass]="{
                        'bg-warning text-dark': p.estado === 'Activo' && !p.estaVencido,
                        'bg-danger': p.estaVencido,
                        'bg-success': p.estado === 'Devuelto',
                        'bg-secondary': p.estado === 'Cancelado'
                      }">
                        {{ p.estaVencido ? 'Vencido' : p.estado }}
                      </span>
                    </td>
                    <td class="text-end pe-3">
                      @if (p.estado === 'Activo') {
                        <div class="btn-group btn-group-sm">
                          @if (authService.hasPermission('Prestamos.Devolver')) {
                            <button class="btn btn-outline-success" (click)="openDevolucionModal(p)" title="Registrar Devolución">
                              <i class="bi bi-box-arrow-in-left me-1"></i> Devolver
                            </button>
                          }
                          @if (authService.hasPermission('Prestamos.Cancelar')) {
                            <button class="btn btn-outline-danger" (click)="openCancelarModal(p)" title="Cancelar Préstamo">
                              <i class="bi bi-x-circle me-1"></i> Cancelar
                            </button>
                          }
                        </div>
                      } @else {
                        <span class="text-muted small">Cerrado</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="card-footer bg-white border-0 px-3">
            <app-pagination
              [page]="prestamosResult.page"
              [pageSize]="prestamosResult.pageSize"
              [totalCount]="prestamosResult.totalCount"
              [totalPages]="prestamosResult.totalPages"
              [hasPrevious]="prestamosResult.hasPrevious"
              [hasNext]="prestamosResult.hasNext"
              (pageChange)="onPageChange($event)"
              (pageSizeChange)="onPageSizeChange($event)"
            ></app-pagination>
          </div>
        </div>
      }

      <!-- Modal Registrar Devolución -->
      @if (showDevolucionModal && selectedPrestamo) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold text-success"><i class="bi bi-box-arrow-in-left me-2"></i>Registrar Devolución</h5>
                <button type="button" class="btn-close" (click)="showDevolucionModal = false"></button>
              </div>

              <div class="modal-body p-4">
                <p>Libro: <strong>{{ selectedPrestamo.libroTitulo }}</strong></p>
                <p>Lector: <strong>{{ selectedPrestamo.lectorNombreCompleto }}</strong></p>

                <div class="mb-3">
                  <label class="form-label fw-semibold">Observaciones de devolución (opcional)</label>
                  <textarea class="form-control" rows="2" [(ngModel)]="devolucionObs" placeholder="Ej. Devuelto a tiempo, libro en excelente estado"></textarea>
                </div>
              </div>

              <div class="modal-footer border-0">
                <button class="btn btn-light" (click)="showDevolucionModal = false">Cerrar</button>
                <button class="btn btn-success" (click)="confirmarDevolucion()" [disabled]="isSaving">
                  @if (isSaving) {
                    <span class="spinner-border spinner-border-sm me-1"></span> Procesando...
                  } @else {
                    <i class="bi bi-check2 me-1"></i> Confirmar Devolución
                  }
                </button>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- Modal Cancelar Préstamo -->
      @if (showCancelarModal && selectedPrestamo) {
        <div class="modal fade show d-block bg-dark bg-opacity-50" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow rounded-4">
              <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-bold text-danger"><i class="bi bi-x-circle me-2"></i>Cancelar Préstamo</h5>
                <button type="button" class="btn-close" (click)="showCancelarModal = false"></button>
              </div>

              <div class="modal-body p-4">
                <p>¿Está seguro de cancelar el préstamo de <strong>{{ selectedPrestamo.libroTitulo }}</strong>?</p>
                <div class="mb-3">
                  <label class="form-label fw-semibold">Motivo de la cancelación *</label>
                  <textarea class="form-control" rows="2" [(ngModel)]="cancelarMotivo" required placeholder="Ej. Solicitado por error del usuario"></textarea>
                </div>
              </div>

              <div class="modal-footer border-0">
                <button class="btn btn-light" (click)="showCancelarModal = false">No, mantener</button>
                <button class="btn btn-danger" (click)="confirmarCancelacion()" [disabled]="isSaving || !cancelarMotivo">
                  @if (isSaving) {
                    <span class="spinner-border spinner-border-sm me-1"></span> Cancelando...
                  } @else {
                    <i class="bi bi-trash me-1"></i> Sí, Cancelar Préstamo
                  }
                </button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class PrestamosComponent implements OnInit {
  private prestamosService = inject(PrestamosService);
  public authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  prestamosResult: PagedResult<Prestamo> = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false };
  currentTab: 'todos' | 'activos' | 'vencidos' = 'todos';
  isLoading = false;
  isSaving = false;

  alertMessage = '';
  alertClass = 'alert-info';
  alertIcon = 'bi-info-circle';

  showDevolucionModal = false;
  devolucionObs = '';

  showCancelarModal = false;
  cancelarMotivo = '';

  selectedPrestamo: Prestamo | null = null;

  ngOnInit(): void {
    this.loadPrestamos();
  }

  setTab(tab: 'todos' | 'activos' | 'vencidos'): void {
    this.currentTab = tab;
    this.loadPrestamos(1);
  }

  loadPrestamos(page = 1, pageSize = 10): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    if (this.currentTab === 'activos') {
      this.prestamosService.getActivos(page, pageSize).subscribe({
        next: (res) => {
          this.prestamosResult = res;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => this.handleError(err)
      });
    } else if (this.currentTab === 'vencidos') {
      this.prestamosService.getVencidos(page, pageSize).subscribe({
        next: (res) => {
          this.prestamosResult = res;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => this.handleError(err)
      });
    } else {
      this.prestamosService.getAll({ page, pageSize }).subscribe({
        next: (res) => {
          this.prestamosResult = res;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: (err) => this.handleError(err)
      });
    }
  }

  onPageChange(page: number): void {
    this.loadPrestamos(page, this.prestamosResult.pageSize);
  }

  onPageSizeChange(size: number): void {
    this.loadPrestamos(1, size);
  }

  openDevolucionModal(p: Prestamo): void {
    this.selectedPrestamo = p;
    this.devolucionObs = '';
    this.showDevolucionModal = true;
  }

  confirmarDevolucion(): void {
    if (!this.selectedPrestamo) return;
    this.isSaving = true;

    this.prestamosService.registrarDevolucion(this.selectedPrestamo.id, this.devolucionObs).subscribe({
      next: () => {
        this.isSaving = false;
        this.showDevolucionModal = false;
        this.showAlert(`Devolución registrada exitosamente para '${this.selectedPrestamo?.libroTitulo}'.`, 'alert-success', 'bi-check-circle');
        this.loadPrestamos(this.prestamosResult.page, this.prestamosResult.pageSize);
      },
      error: (err) => {
        this.isSaving = false;
        this.showAlert('Error al registrar devolución: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
      }
    });
  }

  openCancelarModal(p: Prestamo): void {
    this.selectedPrestamo = p;
    this.cancelarMotivo = '';
    this.showCancelarModal = true;
  }

  confirmarCancelacion(): void {
    if (!this.selectedPrestamo) return;
    this.isSaving = true;

    this.prestamosService.cancelarPrestamo(this.selectedPrestamo.id, this.cancelarMotivo).subscribe({
      next: () => {
        this.isSaving = false;
        this.showCancelarModal = false;
        this.showAlert(`Préstamo de '${this.selectedPrestamo?.libroTitulo}' cancelado exitosamente.`, 'alert-warning', 'bi-x-circle');
        this.loadPrestamos(this.prestamosResult.page, this.prestamosResult.pageSize);
      },
      error: (err) => {
        this.isSaving = false;
        this.showAlert('Error al cancelar préstamo: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
      }
    });
  }

  private handleError(err: any): void {
    this.isLoading = false;
    this.showAlert('Error al consultar préstamos: ' + (err.error?.message || err.message), 'alert-danger', 'bi-exclamation-triangle');
  }

  private showAlert(msg: string, cssClass: string, icon: string): void {
    this.alertMessage = msg;
    this.alertClass = cssClass;
    this.alertIcon = icon;
    this.cdr.markForCheck();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
