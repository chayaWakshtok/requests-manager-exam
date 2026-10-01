import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  BehaviorSubject,
  Observable,
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
  merge,
  of,
  scan,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { BulkUpdateResult } from '../../core/models/bulk-update-result.model';
import { LoadState } from '../../core/models/load-state.model';
import { PagedResult } from '../../core/models/paged-result.model';
import { RequestFilters, RequestQuery, SortField } from '../../core/models/request-query.model';
import { RequestStats } from '../../core/models/request-stats.model';
import { RequestStatus } from '../../core/models/request-status.model';
import { ServiceRequest } from '../../core/models/service-request.model';
import { StatusHistoryEntry } from '../../core/models/status-history-entry.model';
import { toErrorMessage } from '../../core/services/api-error';
import { RequestsApiService } from '../../core/services/requests-api.service';

export const DEFAULT_QUERY: RequestQuery = {
  search: '',
  status: [],
  priority: [],
  organizationName: '',
  assignedTo: '',
  createdFrom: '',
  createdTo: '',
  sortBy: 'createdAt',
  sortDir: 'desc',
  page: 1,
  pageSize: 20,
};

export interface Notice {
  kind: 'success' | 'warning' | 'error';
  text: string;
}

/**
 * State of the requests screen (provided per page component).
 * All searching, filtering, sorting and paging happens on the server; this class only
 * holds the query and turns it into HTTP calls. switchMap cancels an in-flight request
 * when a newer query arrives, so a slow old response can never overwrite a newer one.
 */
@Injectable()
export class RequestsStore {
  private readonly api = inject(RequestsApiService);

  private readonly query$ = new BehaviorSubject<RequestQuery>(DEFAULT_QUERY);
  private readonly searchInput$ = new Subject<string>();
  private readonly reloadList$ = new Subject<void>();
  private readonly reloadStats$ = new Subject<void>();
  private readonly historyFor$ = new BehaviorSubject<number | null>(null);

  readonly query = toSignal(this.query$, { requireSync: true });
  readonly notice = signal<Notice | null>(null);
  readonly busyIds = signal<ReadonlySet<number>>(new Set());
  /** Selected rows for bulk update: id -> rowVersion the user saw. */
  readonly selection = signal<ReadonlyMap<number, string>>(new Map());
  readonly bulkResult = signal<BulkUpdateResult | null>(null);

  readonly list = toSignal(
    merge(this.query$, this.reloadList$.pipe(map(() => this.query$.value))).pipe(
      tap(() => this.selection.set(new Map())),
      switchMap((query) => this.load(this.api.search(query))),
      scan(mergeState<PagedResult<ServiceRequest>>, initialState()),
    ),
    { initialValue: initialState<PagedResult<ServiceRequest>>() },
  );

  readonly stats = toSignal(
    this.reloadStats$.pipe(
      startWith(undefined),
      switchMap(() => this.load(this.api.getStats())),
      scan(mergeState<RequestStats>, initialState()),
    ),
    { initialValue: initialState<RequestStats>() },
  );

  readonly historyRequestId = toSignal(this.historyFor$, { requireSync: true });
  readonly history = toSignal(
    this.historyFor$.pipe(
      switchMap((id) => (id === null ? of(initialState<StatusHistoryEntry[]>()) : this.load(this.api.getHistory(id)))),
      scan(mergeState<StatusHistoryEntry[]>, initialState()),
    ),
    { initialValue: initialState<StatusHistoryEntry[]>() },
  );

  constructor() {
    // Debounce typing; ignore 1-character terms (the API requires >= 2); skip repeats.
    this.searchInput$
      .pipe(
        map((term) => term.trim()),
        debounceTime(350),
        filter((term) => term.length === 0 || term.length >= 2),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((search) => this.patchQuery({ search, page: 1 }));
  }

  // ---- query ----------------------------------------------------------------------------

  setSearch(term: string): void {
    this.searchInput$.next(term);
  }

  setFilters(filters: RequestFilters): void {
    this.patchQuery({ ...filters, page: 1 });
  }

  toggleSort(field: SortField): void {
    const { sortBy, sortDir } = this.query$.value;
    const dir = sortBy === field && sortDir === 'desc' ? 'asc' : 'desc';
    this.patchQuery({ sortBy: field, sortDir: dir, page: 1 });
  }

  setPage(page: number): void {
    this.patchQuery({ page });
  }

  setPageSize(pageSize: number): void {
    this.patchQuery({ pageSize, page: 1 });
  }

  refresh(): void {
    this.reloadList$.next();
    this.reloadStats$.next();
    const historyId = this.historyFor$.value;
    if (historyId !== null) this.historyFor$.next(historyId);
  }

  // ---- history --------------------------------------------------------------------------

  showHistory(id: number | null): void {
    this.historyFor$.next(id);
  }

  // ---- status updates -------------------------------------------------------------------

  updateStatus(row: ServiceRequest, status: RequestStatus): void {
    this.setBusy(row.id, true);
    this.api.updateStatus(row.id, status, row.rowVersion).subscribe({
      next: () => {
        this.setBusy(row.id, false);
        this.notice.set({ kind: 'success', text: `פנייה ${row.id} עודכנה.` });
        this.refresh();
      },
      error: (error: unknown) => {
        this.setBusy(row.id, false);
        if (error instanceof HttpErrorResponse && error.status === 409) {
          // Someone else changed it: show the fresh data and let the user decide again.
          this.notice.set({
            kind: 'warning',
            text: `פנייה ${row.id} עודכנה בינתיים על ידי משתמש אחר. הנתונים רועננו, נא לבחור שוב.`,
          });
          this.refresh();
        } else {
          this.notice.set({ kind: 'error', text: toErrorMessage(error) });
        }
      },
    });
  }

  toggleSelected(row: ServiceRequest): void {
    const next = new Map(this.selection());
    if (next.has(row.id)) next.delete(row.id);
    else next.set(row.id, row.rowVersion);
    this.selection.set(next);
  }

  clearSelection(): void {
    this.selection.set(new Map());
  }

  setAllSelected(rows: ServiceRequest[], selected: boolean): void {
    this.selection.set(selected ? new Map(rows.map((r) => [r.id, r.rowVersion])) : new Map());
  }

  bulkUpdate(status: RequestStatus): void {
    const items = [...this.selection()].map(([id, rowVersion]) => ({ id, rowVersion }));
    if (items.length === 0) return;

    this.api.bulkUpdateStatus(status, items).subscribe({
      next: (result) => {
        this.bulkResult.set(result);
        this.notice.set({
          kind: result.failed === 0 ? 'success' : 'warning',
          text: `עדכון מרובה: ${result.succeeded} מתוך ${result.requested} עודכנו.`,
        });
        this.refresh();
      },
      error: (error: unknown) => this.notice.set({ kind: 'error', text: toErrorMessage(error) }),
    });
  }

  // ---- helpers --------------------------------------------------------------------------

  private patchQuery(patch: Partial<RequestQuery>): void {
    this.query$.next({ ...this.query$.value, ...patch });
  }

  private setBusy(id: number, busy: boolean): void {
    const next = new Set(this.busyIds());
    if (busy) next.add(id);
    else next.delete(id);
    this.busyIds.set(next);
  }

  /** Wraps a request into loading -> data | error patches for scan(mergeState). */
  private load<T>(source: Observable<T>): Observable<Partial<LoadState<T>>> {
    return source.pipe(
      map((data) => ({ loading: false, data, error: null })),
      catchError((error: unknown) => of({ loading: false, data: null, error: toErrorMessage(error) })),
      startWith({ loading: true, error: null }),
    );
  }
}

function initialState<T>(): LoadState<T> {
  return { loading: false, data: null, error: null };
}

/** While loading, the previous data stays on screen (no flicker); errors replace it. */
function mergeState<T>(state: LoadState<T>, patch: Partial<LoadState<T>>): LoadState<T> {
  return { ...state, ...patch };
}
