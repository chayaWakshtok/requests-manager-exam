import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LoadState } from '../../../../core/models/load-state.model';
import { PRIORITY_LABELS } from '../../../../core/models/request-priority.model';
import { CountByKey, RequestStats } from '../../../../core/models/request-stats.model';
import { STATUS_LABELS } from '../../../../core/models/request-status.model';

@Component({
  selector: 'app-stats-panel',
  imports: [DatePipe, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stats-panel.component.html',
  styleUrl: './stats-panel.component.scss',
})
export class StatsPanelComponent {
  readonly state = input.required<LoadState<RequestStats>>();
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;

  /** Share of a count out of a total, in percent (for the bars). */
  protected percent(count: number, total: number): number {
    return total > 0 ? (count / total) * 100 : 0;
  }

  protected sum<T>(items: CountByKey<T>[]): number {
    return items.reduce((acc, x) => acc + x.count, 0);
  }

  protected max<T>(items: CountByKey<T>[]): number {
    return items.reduce((acc, x) => Math.max(acc, x.count), 0);
  }
}
