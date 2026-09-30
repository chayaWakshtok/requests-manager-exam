import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LoadState, STATUS_LABELS, StatusHistoryEntry } from '../../../core/models/request.models';

@Component({
  selector: 'app-history-panel',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside>
      <header>
        <h3>היסטוריית סטטוסים - פנייה {{ requestId() }}</h3>
        <button type="button" (click)="closed.emit()" aria-label="סגירה">✕</button>
      </header>
      @let s = state();
      @if (s.loading) {
        <div class="muted">טוען...</div>
      } @else if (s.error) {
        <div class="error">{{ s.error }}</div>
      } @else if (s.data?.length === 0) {
        <div class="muted">לא בוצעו שינויי סטטוס בפנייה זו.</div>
      } @else {
        <ol>
          @for (h of s.data; track h.id) {
            <li>
              <b>{{ statusLabels[h.previousStatus] }} ← {{ statusLabels[h.newStatus] }}</b>
              <div class="muted">{{ h.changedAt | date: 'dd/MM/yyyy HH:mm:ss' }} · {{ h.changedBy }}</div>
            </li>
          }
        </ol>
      }
    </aside>
  `,
  styles: `
    aside { border: 1px solid #e3e6eb; border-radius: 6px; padding: 12px 16px; background: #fff; }
    header { display: flex; justify-content: space-between; align-items: center; }
    h3 { font-size: 15px; margin: 0; }
    header button { all: unset; cursor: pointer; padding: 4px; }
    ol { padding-inline-start: 18px; }
    li { margin-bottom: 8px; }
    .muted { color: #8a8f98; font-size: 13px; }
    .error { color: #b91c1c; }
  `,
})
export class HistoryPanelComponent {
  readonly requestId = input.required<number>();
  readonly state = input.required<LoadState<StatusHistoryEntry[]>>();
  readonly closed = output<void>();
  protected readonly statusLabels = STATUS_LABELS;
}
