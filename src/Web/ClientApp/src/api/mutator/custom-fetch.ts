/**
 * Custom fetch used by the Orval-generated client.
 *
 * - Always sends cookies (`credentials: 'include'`) so the ASP.NET Identity application
 *   cookie flows with every request (matches the previous NSwag `requestCredentials: include`).
 * - Parses the response body and, on a non-2xx status, throws an {@link ApiError} carrying the
 *   parsed RFC 9110 ProblemDetails / HttpValidationProblemDetails body produced by the API.
 * - On network failure (no response at all), throws a {@link NetworkError} so the UI can
 *   distinguish connectivity issues from API errors.
 * - Returns binary payloads (PDF/XLSX/CSV exports) as a Blob instead of trying to parse them.
 */

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
  errorCode?: string;
  [key: string]: unknown;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem?: ProblemDetails;

  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.detail || problem?.title || `Request failed with status ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export class NetworkError extends Error {
  constructor(message?: string) {
    super(message ?? 'Impossible de se connecter au serveur. Vérifiez votre connexion internet.');
    this.name = 'NetworkError';
  }
}

// Content types returned as-is in a Blob (non-JSON binaries: PDF reports, spreadsheet or CSV
// exports, signed documents). The caller triggers a download via URL.createObjectURL.
const BINARY_CONTENT_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'text/csv',
  'application/octet-stream',
];

async function parseBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type') ?? '';
  if (response.status === 204 || response.status === 205) return undefined;
  if (BINARY_CONTENT_TYPES.some(ct => contentType.includes(ct))) {
    return await response.blob();
  }
  if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
    const text = await response.text();
    return text ? JSON.parse(text) : undefined;
  }
  const text = await response.text();
  return text || undefined;
}

export const customFetch = async <T>(url: string, options: RequestInit): Promise<T> => {
  let response: Response;
  try {
    response = await fetch(url, {
      ...options,
      credentials: 'include',
      // React Query already caches at the application level; the browser HTTP cache is not just
      // redundant here but harmful: an index.html once served (with an ETag) under an /api URL
      // stays pinned there and is replayed as a 304 on every revalidation.
      cache: 'no-store',
    });
  } catch (original: unknown) {
    throw new NetworkError(
      original instanceof TypeError ? original.message : undefined,
    );
  }

  // SPA fallback (index.html) served instead of the API: backend unreachable (dev without the
  // proxy) or a non-existent API route (version skew). Never a success.
  const contentType = response.headers.get('content-type') ?? '';
  if (response.ok && contentType.includes('text/html')) {
    throw new NetworkError('Le serveur a renvoyé une page au lieu des données attendues.');
  }

  const body = await parseBody(response);

  if (!response.ok) {
    throw new ApiError(response.status, body as ProblemDetails | undefined);
  }

  // Orval's fetch client expects the composite { status, data, headers } shape.
  return {
    status: response.status,
    data: body,
    headers: response.headers,
  } as T;
};
