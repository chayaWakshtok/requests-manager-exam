export type BulkOutcome = 'Updated' | 'NotFound' | 'Conflict' | 'InvalidTransition';

export interface BulkUpdateResult {
  requested: number;
  succeeded: number;
  failed: number;
  results: { id: number; outcome: BulkOutcome; rowVersion: string | null; error: string | null }[];
}
