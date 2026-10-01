import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime } from 'rxjs';
import { PRIORITIES, PRIORITY_LABELS, RequestPriority } from '../../../../core/models/request-priority.model';
import { RequestFilters } from '../../../../core/models/request-query.model';
import { RequestStatus, STATUSES, STATUS_LABELS } from '../../../../core/models/request-status.model';

/** Presentational: owns the form, emits search text and filter values. Knows nothing about HTTP. */
@Component({
  selector: 'app-request-filters',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './request-filters.component.html',
  styleUrl: './request-filters.component.scss',
})
export class RequestFiltersComponent {
  readonly searchChange = output<string>();
  readonly filtersChange = output<RequestFilters>();

  protected readonly statuses = STATUSES;
  protected readonly priorities = PRIORITIES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly form = new FormGroup({
    status: new FormControl<RequestStatus | ''>('', { nonNullable: true }),
    priority: new FormControl<RequestPriority | ''>('', { nonNullable: true }),
    organizationName: new FormControl('', { nonNullable: true }),
    assignedTo: new FormControl('', { nonNullable: true }),
    createdFrom: new FormControl('', { nonNullable: true }),
    createdTo: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    // The store debounces the free-text search itself; here only raw keystrokes are forwarded.
    this.search.valueChanges.pipe(takeUntilDestroyed()).subscribe((v) => this.searchChange.emit(v));

    // Text boxes inside the form are debounced too, so typing an organization doesn't fire per key.
    this.form.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => {
      const v = this.form.getRawValue();
      this.filtersChange.emit({
        status: v.status ? [v.status] : [],
        priority: v.priority ? [v.priority] : [],
        organizationName: v.organizationName.trim(),
        assignedTo: v.assignedTo.trim(),
        createdFrom: v.createdFrom,
        createdTo: v.createdTo,
      });
    });
  }

  protected clear(): void {
    this.search.setValue('');
    this.form.reset();
  }
}
