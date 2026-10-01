import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LoadState } from '../../../../core/models/load-state.model';
import { STATUS_LABELS } from '../../../../core/models/request-status.model';
import { StatusHistoryEntry } from '../../../../core/models/status-history-entry.model';

@Component({
  selector: 'app-history-panel',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './history-panel.component.html',
  styleUrl: './history-panel.component.scss',
})
export class HistoryPanelComponent {
  readonly requestId = input.required<number>();
  readonly state = input.required<LoadState<StatusHistoryEntry[]>>();
  readonly closed = output<void>();
  protected readonly statusLabels = STATUS_LABELS;
}
