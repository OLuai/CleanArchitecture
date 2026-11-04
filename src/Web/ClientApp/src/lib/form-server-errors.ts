/**
 * Applies server errors (ProblemDetails) to a TanStack Form.
 *
 * - Validation errors that map to a rendered field (key present in the form values) are written
 *   to that field's `onServer` error slot, so they show up under the field.
 * - Errors that cannot be attached to a single field (object-level key, unknown key, or a
 *   business error with no `errors` dictionary) are returned as general messages, to be shown in
 *   a banner above the form (`FormErrorSummary`) and/or a toast.
 *
 * Field names are derived from `form.state.values` (top-level keys), which covers every form
 * without depending on the shape of its zod schema.
 */
import type { AnyFormApi } from '@tanstack/react-form';

import { apiErrorMessage, apiFieldErrors } from '@/lib/api-error';

function fieldNamesOf(form: AnyFormApi): string[] {
  return Object.keys((form.state.values ?? {}) as Record<string, unknown>);
}

// A field only has meta once it has been mounted (rendered through <form.Field>). Keys that exist
// in the values but were never rendered have none, and setFieldMeta would throw on them
// (prev === undefined). Only target fields that are actually mounted.
function setOnServer(form: AnyFormApi, name: string, onServer: string | undefined): boolean {
  const prev = form.getFieldMeta(name);
  if (!prev) return false;
  form.setFieldMeta(name, (m) => ({
    ...m,
    ...(onServer !== undefined ? { isTouched: true } : {}),
    errorMap: { ...m.errorMap, onServer },
  }));
  return true;
}

/** Clears previous server errors (the `onServer` slot) from every mounted field. */
export function clearServerErrors(form: AnyFormApi): void {
  for (const name of fieldNamesOf(form)) {
    setOnServer(form, name, undefined);
  }
}

/**
 * Routes an API response's errors onto the form's fields and returns the general messages
 * (to display in a banner and/or a toast).
 */
export function applyServerErrors(
  form: AnyFormApi,
  error: unknown,
  fallback = "L'opération a échoué.",
): string[] {
  const fieldNames = fieldNamesOf(form);
  clearServerErrors(form);

  const errors = apiFieldErrors(error);
  const general: string[] = [];

  for (const [key, messages] of Object.entries(errors)) {
    // Attach to the field when it exists AND is mounted; otherwise (unrendered field, object-level
    // or unknown key) treat it as a general message.
    const attached = fieldNames.includes(key) && setOnServer(form, key, messages.join(', '));
    if (!attached) {
      general.push(...messages);
    }
  }

  // No field dictionary at all (business error, 404, network failure…) → one general message.
  if (Object.keys(errors).length === 0) {
    general.push(apiErrorMessage(error, fallback));
  }

  return general;
}
