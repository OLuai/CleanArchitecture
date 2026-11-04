/**
 * Turns an API error into something displayable. Validation ProblemDetails (FluentValidation)
 * carry an `errors` dictionary (field → messages); otherwise fall back to `detail`/`title`.
 * Used to surface failures through a toast (sonner).
 */
import { ApiError, NetworkError } from '@/api/mutator/custom-fetch';

export function apiErrorMessage(e: unknown, fallback: string): string {
  if (e instanceof NetworkError) {
    return e.message;
  }
  if (e instanceof ApiError) {
    if (e.problem?.errors) {
      const messages = Object.values(e.problem.errors).flat().filter(Boolean);
      if (messages.length > 0) return messages.join(' · ');
    }
    if (e.problem?.detail) return e.problem.detail;
    if (e.problem?.title) return e.problem.title;
  }
  return fallback;
}

/**
 * Per-field validation errors (camelCase keys, as produced by the backend), or an empty object
 * when the error is not a field-level validation failure (business error, network error…).
 * Used to route messages to the field they belong to — see `applyServerErrors`.
 */
export function apiFieldErrors(e: unknown): Record<string, string[]> {
  if (e instanceof ApiError && e.problem?.errors) {
    return e.problem.errors;
  }
  return {};
}
