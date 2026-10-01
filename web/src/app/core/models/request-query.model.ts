import { RequestPriority } from './request-priority.model';
import { RequestStatus } from './request-status.model';

export type SortField = 'createdAt' | 'updatedAt' | 'priority' | 'status' | 'title' | 'organizationName';
export type SortDir = 'asc' | 'desc';

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
