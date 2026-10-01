import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { BulkUpdateResult } from '../../../../core/models/bulk-update-result.model';
import { RequestStatus, STATUSES, STATUS_LABELS } from '../../../../core/models/request-status.model';

@Component({
  selector: 'app-bulk-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bulk-bar.component.html',
  styleUrl: './bulk-bar.component.scss',
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
