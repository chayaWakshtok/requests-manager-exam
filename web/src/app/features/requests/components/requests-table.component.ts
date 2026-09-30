import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import {
  NEXT_STATUSES,
  PRIORITY_LABELS,
  RequestStatus,
  STATUS_LABELS,
  ServiceRequest,
  SortDir,
  SortField,
} from '../../../core/models/request.models';

/** Presentational table: renders one server page and emits user intents. */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <table>
      <thead>
        <tr>
          <th class="narrow">
            <input type="checkbox" [checked]="allSelected()" (change)="selectAll.emit($any($event.target).checked)"
                   aria-label="בחירת כל הדף" />
          </th>
          <th class="narrow">#</th>
          @for (col of columns; track col.field) {
            <th>
              <button class="sort" type="button" (click)="sortChange.emit(col.field)">
                {{ col.label }}
                @if (sortBy() === col.field) {
                  <span aria-hidden="true">{{ sortDir() === 'asc' ? '▲' : '▼' }}</span>
                }
              </button>
            </th>
          }
          <th>מטפל</th>
          <th>שינוי סטטוס</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track row.id) {
          <tr [class.selected]="selectedIds().has(row.id)">
            <td><input type="checkbox" [checked]="selectedIds().has(row.id)" (change)="toggleSelect.emit(row)" /></td>
            <td>{{ row.id }}</td>
            <td>{{ row.title }}</td>
            <td>{{ row.organizationName }}</td>
            <td><span class="badge" [attr.data-status]="row.status">{{ statusLabels[row.status] }}</span></td>
            <td><span class="prio" [attr.data-priority]="row.priority">{{ priorityLabels[row.priority] }}</span></td>
            <td>{{ row.createdAt | date: 'dd/MM/yyyy HH:mm' }}</td>
            <td>{{ row.updatedAt | date: 'dd/MM/yyyy HH:mm' }}</td>
            <td>{{ row.assignedTo ?? '—' }}</td>
            <td>
              @if (nextStatuses[row.status].length) {
                <select [disabled]="busyIds().has(row.id)" (change)="onStatusSelected(row, $any($event.target))">
                  <option value="">בחירה...</option>
                  @for (s of nextStatuses[row.status]; track s) {
                    <option [value]="s">{{ statusLabels[s] }}</option>
                  }
                </select>
              } @else {
                <span class="muted">סופי</span>
              }
            </td>
            <td><button type="button" class="link" (click)="showHistory.emit(row)">היסטוריה</button></td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    table { width: 100%; border-collapse: collapse; font-size: 14px; }
    th, td { padding: 6px 8px; border-bottom: 1px solid #e3e6eb; text-align: start; }
    th { background: #f5f6f8; font-weight: 600; white-space: nowrap; }
    .narrow { width: 1%; }
    tr.selected { background: #eef4ff; }
    .sort { all: unset; cursor: pointer; }
    .link { all: unset; cursor: pointer; color: #1a56db; }
    .muted { color: #8a8f98; }
    .badge { padding: 2px 8px; border-radius: 10px; background: #eceff3; font-size: 12px; }
    .badge[data-status='New'] { background: #e0ecff; }
    .badge[data-status='InProgress'] { background: #fff4d6; }
    .badge[data-status='Waiting'] { background: #f3e8ff; }
    .badge[data-status='Completed'] { background: #dcfce7; }
    .prio[data-priority='High'] { color: #b91c1c; font-weight: 600; }
  `,
})
export class RequestsTableComponent {
  readonly rows = input.required<ServiceRequest[]>();
  readonly sortBy = input.required<SortField>();
  readonly sortDir = input.required<SortDir>();
  readonly selectedIds = input.required<ReadonlySet<number>>();
  readonly busyIds = input.required<ReadonlySet<number>>();

  readonly sortChange = output<SortField>();
  readonly statusChange = output<{ row: ServiceRequest; status: RequestStatus }>();
  readonly showHistory = output<ServiceRequest>();
  readonly toggleSelect = output<ServiceRequest>();
  readonly selectAll = output<boolean>();

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;
  protected readonly nextStatuses = NEXT_STATUSES;
  protected readonly columns: { field: SortField; label: string }[] = [
    { field: 'title', label: 'כותרת' },
    { field: 'organizationName', label: 'ארגון' },
    { field: 'status', label: 'סטטוס' },
    { field: 'priority', label: 'עדיפות' },
    { field: 'createdAt', label: 'נוצרה' },
    { field: 'updatedAt', label: 'עודכנה' },
  ];

  protected readonly allSelected = computed(
    () => this.rows().length > 0 && this.rows().every((r) => this.selectedIds().has(r.id)),
  );

  protected onStatusSelected(row: ServiceRequest, select: HTMLSelectElement): void {
    const status = select.value as RequestStatus | '';
    select.value = '';
    if (status) this.statusChange.emit({ row, status });
  }
}
