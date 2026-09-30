import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  BulkUpdateResult,
  PagedResult,
  RequestQuery,
  RequestStats,
  RequestStatus,
  ServiceRequest,
  StatusHistoryEntry,
} from '../models/request.models';

/** The only place that knows the API's URLs and parameter names. */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/requests';

  search(query: RequestQuery): Observable<PagedResult<ServiceRequest>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDir', query.sortDir);

    query.status.forEach((s) => (params = params.append('status', s)));
    query.priority.forEach((p) => (params = params.append('priority', p)));
    if (query.search) params = params.set('search', query.search);
    if (query.organizationName) params = params.set('organizationName', query.organizationName);
    if (query.assignedTo) params = params.set('assignedTo', query.assignedTo);
    if (query.createdFrom) params = params.set('createdFrom', query.createdFrom);
    if (query.createdTo) params = params.set('createdTo', `${query.createdTo}T23:59:59`);

    return this.http.get<PagedResult<ServiceRequest>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<ServiceRequest> {
    return this.http.get<ServiceRequest>(`${this.baseUrl}/${id}`);
  }

  getHistory(id: number): Observable<StatusHistoryEntry[]> {
    return this.http.get<StatusHistoryEntry[]>(`${this.baseUrl}/${id}/history`);
  }

  getStats(): Observable<RequestStats> {
    return this.http.get<RequestStats>(`${this.baseUrl}/stats`);
  }

  updateStatus(id: number, status: RequestStatus, rowVersion: string): Observable<ServiceRequest> {
    return this.http.patch<ServiceRequest>(`${this.baseUrl}/${id}/status`, { status, rowVersion });
  }

  bulkUpdateStatus(status: RequestStatus, items: { id: number; rowVersion: string }[]): Observable<BulkUpdateResult> {
    return this.http.post<BulkUpdateResult>(`${this.baseUrl}/bulk-status`, { status, items });
  }
}
