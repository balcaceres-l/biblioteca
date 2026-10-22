import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 my-3">
      <div class="d-flex align-items-center gap-2 text-muted small">
        <span>Mostrando página <strong>{{ page }}</strong> de <strong>{{ totalPages || 1 }}</strong> ({{ totalCount }} registros)</span>
        <span class="ms-2">| Mostrar:</span>
        <select class="form-select form-select-sm w-auto" [ngModel]="pageSize" (ngModelChange)="onPageSizeChange($event)">
          <option [value]="5">5</option>
          <option [value]="10">10</option>
          <option [value]="20">20</option>
          <option [value]="50">50</option>
        </select>
      </div>

      <nav aria-label="Navegación de páginas">
        <ul class="pagination pagination-sm mb-0">
          <li class="page-item" [class.disabled]="!hasPrevious || page <= 1">
            <button class="page-link" (click)="goTo(1)" aria-label="Primera">
              <i class="bi bi-chevron-double-left"></i>
            </button>
          </li>
          <li class="page-item" [class.disabled]="!hasPrevious">
            <button class="page-link" (click)="goTo(page - 1)" aria-label="Anterior">
              <i class="bi bi-chevron-left"></i>
            </button>
          </li>

          @for (p of visiblePages; track p) {
            <li class="page-item" [class.active]="p === page">
              <button class="page-link" (click)="goTo(p)">{{ p }}</button>
            </li>
          }

          <li class="page-item" [class.disabled]="!hasNext">
            <button class="page-link" (click)="goTo(page + 1)" aria-label="Siguiente">
              <i class="bi bi-chevron-right"></i>
            </button>
          </li>
          <li class="page-item" [class.disabled]="!hasNext || page >= totalPages">
            <button class="page-link" (click)="goTo(totalPages)" aria-label="Última">
              <i class="bi bi-chevron-double-right"></i>
            </button>
          </li>
        </ul>
      </nav>
    </div>
  `
})
export class PaginationComponent {
  @Input() page: number = 1;
  @Input() pageSize: number = 10;
  @Input() totalCount: number = 0;
  @Input() totalPages: number = 1;
  @Input() hasPrevious: boolean = false;
  @Input() hasNext: boolean = false;

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get visiblePages(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    let start = Math.max(1, this.page - Math.floor(maxVisible / 2));
    let end = Math.min(this.totalPages, start + maxVisible - 1);

    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  goTo(targetPage: number): void {
    if (targetPage >= 1 && targetPage <= this.totalPages && targetPage !== this.page) {
      this.pageChange.emit(targetPage);
    }
  }

  onPageSizeChange(newSize: number): void {
    this.pageSizeChange.emit(Number(newSize));
  }
}
