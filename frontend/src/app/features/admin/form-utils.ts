import { AbstractControl, ValidationErrors } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

export function notBlank(control: AbstractControl): ValidationErrors | null {
  return typeof control.value === 'string' && !control.value.trim() ? { blank: true } : null;
}
export function validationErrors(error: unknown): string[] {
  if (!(error instanceof HttpErrorResponse) || !error.error?.errors || typeof error.error.errors !== 'object') return [];
  return Object.values(error.error.errors).flat().filter((item): item is string => typeof item === 'string');
}
