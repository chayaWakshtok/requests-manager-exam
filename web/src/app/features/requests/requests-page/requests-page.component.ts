import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { BulkBarComponent } from '../components/bulk-bar/bulk-bar.component';
import { HistoryPanelComponent } from '../components/history-panel/history-panel.component';
import { PaginatorComponent } from '../components/paginator/paginator.component';
import { RequestFiltersComponent } from '../components/request-filters/request-filters.component';
import { RequestsTableComponent } from '../components/requests-table/requests-table.component';
import { StatsPanelComponent } from '../components/stats-panel/stats-panel.component';
import { RequestsStore } from '../requests.store';

/** Container: wires the store to the presentational components. */
@Component({
  selector: 'app-requests-page',
  imports: [
    RequestFiltersComponent,
    RequestsTableComponent,
    PaginatorComponent,
    StatsPanelComponent,
    HistoryPanelComponent,
    BulkBarComponent,
  ],
  providers: [RequestsStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './requests-page.component.html',
  styleUrl: './requests-page.component.scss',
})
export class RequestsPageComponent {
  protected readonly store = inject(RequestsStore);
  protected readonly selectedIds = computed(() => new Set(this.store.selection().keys()));
}
