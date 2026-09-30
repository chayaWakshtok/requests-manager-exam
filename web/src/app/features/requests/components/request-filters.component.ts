import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime } from 'rxjs';
import {
  PRIORITIES,
  PRIORITY_LABELS,
  RequestFilters,
  RequestPriority,
  RequestStatus,
  STATUSES,
  STATUS_LABELS,
} from '../../../core/models/request.models';

/** Presentational: owns the form, emits search text and filter values. Knows nothing about HTTP. */
@Component({
  selector: 'app-request-filters',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="filters" [formGroup]="form" (ngSubmit)="$event.preventDefault()">
      <label class="search">
        חיפוש (כותרת / ארגון)
        <input type="search" [formControl]="search" placeholder="לפחות 2 תווים..." />
      </label>

      <label>
        סטטוס
        <select formControlName="status">
          <option value="">הכל</option>
          @for (s of statuses; track s) {
            <option [value]="s">{{ statusLabels[s] }}</option>
          }
        </select>
      </label>

      <label>
        עדיפות
        <select formControlName="priority">
          <option value="">הכל</option>
          @for (p of priorities; track p) {
            <option [value]="p">{{ priorityLabels[p] }}</option>
          }
        </select>
      </label>

      <label>
        ארגון (מתחיל ב...)
        <input formControlName="organizationName" />
      </label>

      <label>
        מטפל
        <input formControlName="assignedTo" placeholder="agent07" />
      </label>

      <label>
        מתאריך
        <input type="date" formControlName="createdFrom" />
      </label>

      <label>
        עד תאריך
        <input type="date" formControlName="createdTo" />
      </label>

      <button type="button" (click)="clear()">ניקוי</button>
    </form>
  `,
  styles: `
    .filters { display: flex; flex-wrap: wrap; gap: 12px; align-items: end; }
    label { display: flex; flex-direction: column; gap: 4px; font-size: 13px; }
    .search { flex: 1 1 240px; }
    input, select { padding: 6px 8px; border: 1px solid #c8ccd4; border-radius: 4px; font: inherit; }
  `,
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
