export type RequestStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';

export const STATUSES: RequestStatus[] = ['New', 'InProgress', 'Waiting', 'Completed'];

export const STATUS_LABELS: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הושלמה',
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
