export type RequestPriority = 'Low' | 'Medium' | 'High';

export const PRIORITIES: RequestPriority[] = ['Low', 'Medium', 'High'];

export const PRIORITY_LABELS: Record<RequestPriority, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה',
};
