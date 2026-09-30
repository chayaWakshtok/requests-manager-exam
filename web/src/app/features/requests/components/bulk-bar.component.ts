import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { BulkUpdateResult, RequestStatus, STATUSES, STATUS_LABELS } from '../../../core/models/request.models';

@Component({
  selector: 'app-bulk-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bar">
      <span>נבחרו {{ selectedCount() }} פניות</span>
      <select #status>
        @for (s of statuses; track s) {
          <option [value]="s">{{ labels[s] }}</option>
        }
      </select>
      <button type="button" [disabled]="selectedCount() === 0" (click)="apply.emit($any(status.value))">עדכון לכולן</button>
      <button type="button" [disabled]="selectedCount() === 0" (click)="clearSelection.emit()">ביטול בחירה</button>
    </div>
    @if (result(); as r) {
      @if (r.failed > 0) {
        <details class="result">
          <summary>{{ r.failed }} פניות לא עודכנו</summary>
          <ul>
            @for (item of r.results; track item.id) {
              @if (item.outcome !== 'Updated') {
                <li>פנייה {{ item.id }}: {{ outcomeLabels[item.outcome] }}</li>
              }
            }
          </ul>
        </details>
      }
    }
  `,
  styles: `
    .bar { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
    .result { margin-top: 6px; font-size: 13px; }
  `,
})
export class BulkBarComponent {
  readonly selectedCount = input.required<number>();
  readonly result = input<BulkUpdateResult | null>(null);
  readonly apply = output<RequestStatus>();
  readonly clearSelection = output<void>();

  protected readonly statuses = STATUSES.filter((s) => s !== 'New');
  protected readonly labels = STATUS_LABELS;
  protected readonly outcomeLabels = {
    Updated: 'עודכנה',
    NotFound: 'לא נמצאה',
    Conflict: 'עודכנה בינתיים על ידי משתמש אחר',
    InvalidTransition: 'מעבר סטטוס לא חוקי',
  } as const;
}
