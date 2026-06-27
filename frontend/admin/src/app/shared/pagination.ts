import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

// Reusable, presentational pagination bar. The parent owns the data and decides whether to
// re-fetch (server-side) or slice (client-side) when (pageChange)/(pageSizeChange) fire.
@Component({
  selector: 'app-pagination',
  imports: [FormsModule],
  template: `
    @if (total() > 0) {
      <div class="pager">
        <span class="muted">{{ rangeStart() }}–{{ rangeEnd() }} of {{ total() }}</span>
        <div class="pager-ctrls">
          <select class="input" [ngModel]="pageSize()" (ngModelChange)="pageSizeChange.emit(+$event)">
            @for (s of sizes(); track s) { <option [ngValue]="s">{{ s }} / page</option> }
          </select>
          <button class="btn btn-sm" [disabled]="pageNumber() <= 1" (click)="go(pageNumber() - 1)">‹ Prev</button>
          <span class="pageno">Page {{ pageNumber() }} of {{ totalPages() }}</span>
          <button class="btn btn-sm" [disabled]="pageNumber() >= totalPages()" (click)="go(pageNumber() + 1)">Next ›</button>
        </div>
      </div>
    }
  `,
  styles: [`
    .pager { display:flex; align-items:center; justify-content:space-between; gap:16px; padding:16px 20px; border-top:1px solid var(--line); flex-wrap:wrap; }
    .pager-ctrls { display:flex; align-items:center; gap:14px; }
    .pager-ctrls .pageno { padding:0 4px; font-weight:600; white-space:nowrap; }
    .pager-ctrls .btn { min-width:84px; justify-content:center; }
  `],
})
export class Pagination {
  total = input.required<number>();
  pageNumber = input.required<number>();
  pageSize = input.required<number>();
  sizes = input<number[]>([10, 20, 50]);

  pageChange = output<number>();
  pageSizeChange = output<number>();

  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  rangeStart = computed(() => (this.total() === 0 ? 0 : (this.pageNumber() - 1) * this.pageSize() + 1));
  rangeEnd = computed(() => Math.min(this.pageNumber() * this.pageSize(), this.total()));

  go(page: number): void {
    if (page >= 1 && page <= this.totalPages()) this.pageChange.emit(page);
  }
}
