import { RequestPriority } from './request-priority.model';
import { RequestStatus } from './request-status.model';

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
