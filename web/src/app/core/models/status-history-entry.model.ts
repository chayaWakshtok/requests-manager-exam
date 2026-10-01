import { RequestStatus } from './request-status.model';

export interface StatusHistoryEntry {
  id: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}
