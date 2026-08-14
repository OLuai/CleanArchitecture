/**
 * Form error banner, shown above the form for errors that cannot be attached to a single field
 * (object-level errors or business rules). Fed by the general messages returned by
 * `applyServerErrors`. Renders nothing when the list is empty.
 */
export function FormErrorSummary({ messages }: { messages: string[] }) {
  if (messages.length === 0) return null;

  return (
    <div
      role="alert"
      className="border-destructive/50 bg-destructive/10 text-destructive rounded-md border px-3 py-2 text-sm"
    >
      {messages.length === 1 ? (
        <p>{messages[0]}</p>
      ) : (
        <ul className="list-disc space-y-1 pl-5">
          {messages.map((m, i) => (
            <li key={i}>{m}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
