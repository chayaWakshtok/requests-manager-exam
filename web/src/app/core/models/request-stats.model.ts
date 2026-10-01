import { RequestPriority } from './request-priority.model';
import { RequestStatus } from './request-status.model';

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
