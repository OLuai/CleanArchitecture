import type { AnyFieldMeta } from '@tanstack/react-form';

/**
 * Renders a TanStack Form field's errors, from client validators and from the `onServer` slot
 * filled by `applyServerErrors`. Shows nothing until the field has been touched, so a pristine
 * form is not covered in red.
 */
export function FieldError({ meta }: { meta: AnyFieldMeta }) {
  if (!meta.isTouched) return null;

  const messages = meta.errors
    .map(error =>
      typeof error === 'string' ? error : ((error as { message?: string } | null)?.message ?? null),
    )
    .filter((message): message is string => Boolean(message));

  if (messages.length === 0) return null;

  return (
    <p className="text-destructive text-sm" role="alert">
      {messages.join(', ')}
    </p>
  );
}
