import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { PRIORITY_LABELS } from '../../../../core/models/request-priority.model';
import { SortDir, SortField } from '../../../../core/models/request-query.model';
import { NEXT_STATUSES, RequestStatus, STATUS_LABELS } from '../../../../core/models/request-status.model';
import { ServiceRequest } from '../../../../core/models/service-request.model';

/** Presentational table: renders one server page and emits user intents. */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './requests-table.component.html',
  styleUrl: './requests-table.component.scss',
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
