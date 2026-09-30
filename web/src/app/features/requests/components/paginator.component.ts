import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-paginator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pager">
      <button type="button" [disabled]="page() <= 1" (click)="pageChange.emit(1)">ראשון</button>
      <button type="button" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">הקודם</button>
      <span>עמוד {{ page() }} מתוך {{ totalPages() || 1 }} ({{ totalCount().toLocaleString() }} פניות)</span>
      <button type="button" [disabled]="page() >= totalPages()" (click)="pageChange.emit(page() + 1)">הבא</button>
      <button type="button" [disabled]="page() >= totalPages()" (click)="pageChange.emit(totalPages())">אחרון</button>
      <label>
        בעמוד
        <select [value]="pageSize()" (change)="pageSizeChange.emit(+$any($event.target).value)">
          @for (size of sizes; track size) {
            <option [value]="size" [selected]="size === pageSize()">{{ size }}</option>
          }
        </select>
      </label>
    </div>
  `,
  styles: `
    .pager { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; margin-top: 12px; }
  `,
})
export class PaginatorComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected readonly sizes = [10, 20, 50, 100];
}
