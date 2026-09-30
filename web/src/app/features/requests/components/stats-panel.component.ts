import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LoadState, PRIORITY_LABELS, RequestStats, STATUS_LABELS } from '../../../core/models/request.models';

@Component({
  selector: 'app-stats-panel',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let s = state();
    @if (s.error) {
      <div class="error">טעינת הנתונים המסכמים נכשלה: {{ s.error }}</div>
    } @else if (s.data; as stats) {
      <div class="stats" [class.dim]="s.loading">
        <section>
          <h3>לפי סטטוס (סה״כ {{ stats.totalCount.toLocaleString() }})</h3>
          @for (x of stats.byStatus; track x.key) {
            <div class="row"><span>{{ statusLabels[x.key] }}</span><b>{{ x.count.toLocaleString() }}</b></div>
          }
        </section>
        <section>
          <h3>פתוחות לפי עדיפות</h3>
          @for (x of stats.openByPriority; track x.key) {
            <div class="row"><span>{{ priorityLabels[x.key] }}</span><b>{{ x.count.toLocaleString() }}</b></div>
          }
        </section>
        <section>
          <h3>ארגונים עם הכי הרבה פניות פתוחות</h3>
          @for (x of stats.topOrganizationsByOpenRequests; track x.key) {
            <div class="row"><span>{{ x.key }}</span><b>{{ x.count }}</b></div>
          }
        </section>
        <small class="muted">חושב ב-{{ stats.generatedAt | date: 'HH:mm:ss' }}</small>
      </div>
    } @else if (s.loading) {
      <div class="muted">טוען נתונים מסכמים...</div>
    }
  `,
  styles: `
    .stats { display: flex; gap: 24px; flex-wrap: wrap; align-items: start; }
    .stats.dim { opacity: .6; }
    section { min-width: 200px; }
    h3 { font-size: 14px; margin: 0 0 6px; }
    .row { display: flex; justify-content: space-between; gap: 12px; font-size: 14px; padding: 2px 0; }
    .muted { color: #8a8f98; }
    .error { color: #b91c1c; }
  `,
})
export class StatsPanelComponent {
  readonly state = input.required<LoadState<RequestStats>>();
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityLabels = PRIORITY_LABELS;
}
