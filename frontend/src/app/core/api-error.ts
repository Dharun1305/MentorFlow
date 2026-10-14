import { HttpErrorResponse } from '@angular/common/http';

export function errorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  if (error.status === 0) return 'Cannot reach MentorFlow. Check that the backend is running and try again.';
  if (error.status === 403) return 'Your account does not have access to this information.';
  if (error.status === 409) return 'This information has changed. Reload it before trying again.';
  return typeof error.error?.message === 'string' ? error.error.message : fallback;
}
