import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LoadState, PRIORITY_LABELS, RequestStats, STATUS_LABELS } from '../../../../core/models/request.models';

@Component({
  selector: 'app-stats-panel',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stats-panel.component.html',
  styleUrl: './stats-panel.component.scss',
})
export class StatsPanelComponent {
  readonly state = input.required<LoadState<RequestStats>>();
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;
}
