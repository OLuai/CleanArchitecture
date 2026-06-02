/**
 * Custom fetch used by the Orval-generated client.
 *
 * - Always sends cookies (`credentials: 'include'`) so the ASP.NET Identity application
 *   cookie flows with every request (matches the previous NSwag `requestCredentials: include`).
 * - Parses the response body and, on a non-2xx status, throws an {@link ApiError} carrying the
 *   parsed RFC 9110 ProblemDetails / HttpValidationProblemDetails body produced by the API.
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

async function parseBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type') ?? '';
  if (response.status === 204 || response.status === 205) return undefined;
  if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
    const text = await response.text();
    return text ? JSON.parse(text) : undefined;
  }
  const text = await response.text();
  return text || undefined;
}

export const customFetch = async <T>(url: string, options: RequestInit): Promise<T> => {
  const response = await fetch(url, {
    ...options,
    credentials: 'include',
  });

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
