import { HttpErrorResponse } from '@angular/common/http';

/** Turns an HTTP error (RFC 7807 Problem Details from the API) into a message for the user. */
export function toErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'אירעה שגיאה לא צפויה.';
  if (error.status === 0) return 'אין תקשורת עם השרת.';

  const problem = error.error as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  if (problem?.errors) return Object.values(problem.errors).flat().join(' ');
  return problem?.detail ?? problem?.title ?? `שגיאת שרת (${error.status}).`;
}
