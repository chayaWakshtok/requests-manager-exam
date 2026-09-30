export type RequestStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';
export type RequestPriority = 'Low' | 'Medium' | 'High';
export type SortField = 'createdAt' | 'updatedAt' | 'priority' | 'status' | 'title' | 'organizationName';
export type SortDir = 'asc' | 'desc';

export const STATUSES: RequestStatus[] = ['New', 'InProgress', 'Waiting', 'Completed'];
export const PRIORITIES: RequestPriority[] = ['Low', 'Medium', 'High'];

export const STATUS_LABELS: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הושלמה',
};

export const PRIORITY_LABELS: Record<RequestPriority, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה',
};

/**
 * Mirrors the server workflow only to decide which options to show.
 * The server is the authority and answers 422 for a transition that is not allowed.
 */
export const NEXT_STATUSES: Record<RequestStatus, RequestStatus[]> = {
  New: ['InProgress', 'Waiting'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress', 'Completed'],
  Completed: [],
};

export interface ServiceRequest {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RequestFilters {
  status: RequestStatus[];
  priority: RequestPriority[];
  organizationName: string;
  assignedTo: string;
  createdFrom: string;
  createdTo: string;
}

export interface RequestQuery extends RequestFilters {
  search: string;
  sortBy: SortField;
  sortDir: SortDir;
  page: number;
  pageSize: number;
}

export interface StatusHistoryEntry {
  id: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}

export interface CountByKey<T> {
  key: T;
  count: number;
}

export interface RequestStats {
  totalCount: number;
  byStatus: CountByKey<RequestStatus>[];
  openByPriority: CountByKey<RequestPriority>[];
  topOrganizationsByOpenRequests: CountByKey<string>[];
  generatedAt: string;
}

export type BulkOutcome = 'Updated' | 'NotFound' | 'Conflict' | 'InvalidTransition';

export interface BulkUpdateResult {
  requested: number;
  succeeded: number;
  failed: number;
  results: { id: number; outcome: BulkOutcome; rowVersion: string | null; error: string | null }[];
}

/** UI state of an async load: keeps the previous data while a new request is in flight. */
export interface LoadState<T> {
  loading: boolean;
  data: T | null;
  error: string | null;
}
