/**
 * Orval types every response as a union discriminated on `status`, because the OpenAPI document
 * describes the error statuses alongside the successful one. The fetch mutator throws an
 * {@link ApiError} on any non-2xx, so a resolved query or mutation only ever holds the success
 * branch — these helpers state that once, instead of casting at each call site.
 */

type SuccessResponse<T> = Extract<T, { status: 200 | 201 | 204 }>;

export type SuccessData<T> = SuccessResponse<T> extends { data: infer D } ? D : never;

export function successData<T extends { status: number; data: unknown }>(
  response: T | undefined,
): SuccessData<T> | undefined {
  return response?.data as SuccessData<T> | undefined;
}
