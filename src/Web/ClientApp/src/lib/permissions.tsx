/**
 * Frontend permission utility. The `Permission` catalogue and the user's `permissions`
 * list both come from the generated OpenAPI client, so the backend is the single source
 * of truth. Use `usePermissions()` in components or `<Can permission={...}>` to gate
 * rendering; use `ensurePermission()` in TanStack Router `beforeLoad` to gate routes.
 */
import { useMemo, type ReactNode } from 'react';
import { redirect } from '@tanstack/react-router';
import type { QueryClient } from '@tanstack/react-query';

import { Permission, type UserInfoResponse } from '@/api/generated/model';
import {
  getInfoQueryOptions,
  info,
} from '@/api/generated/users/users';
import { useSession } from './auth';

export { Permission };

export type PermissionCheck =
  | Permission
  | readonly Permission[]
  | { any: readonly Permission[] }
  | { all: readonly Permission[] };

function evaluate(granted: ReadonlySet<string>, check: PermissionCheck): boolean {
  if (typeof check === 'string') return granted.has(check);
  if (Array.isArray(check)) return check.some(p => granted.has(p));
  if ('any' in check) return check.any.some(p => granted.has(p));
  if ('all' in check) return check.all.every(p => granted.has(p));
  return false;
}

export function usePermissions() {
  const { user, isLoading, isAuthenticated } = useSession();
  // The mutator throws on non-2xx, so a resolved query is always UserInfoResponse.
  const session = user as UserInfoResponse | undefined;
  const granted = useMemo(
    () => new Set<string>(session?.permissions ?? []),
    [session?.permissions],
  );

  return {
    isLoading,
    isAuthenticated,
    permissions: session?.permissions ?? [],
    has: (p: Permission) => granted.has(p),
    hasAny: (...ps: Permission[]) => ps.some(p => granted.has(p)),
    hasAll: (...ps: Permission[]) => ps.every(p => granted.has(p)),
    check: (c: PermissionCheck) => evaluate(granted, c),
  };
}

export interface CanProps {
  permission?: Permission;
  any?: readonly Permission[];
  all?: readonly Permission[];
  fallback?: ReactNode;
  children: ReactNode;
}

/**
 * Renders `children` if the current user has the required permission(s); otherwise renders
 * `fallback` (default: nothing). Pass one of `permission`, `any`, or `all`.
 */
export function Can({ permission, any, all, fallback = null, children }: CanProps) {
  const { check } = usePermissions();
  const ok =
    (permission !== undefined && check(permission)) ||
    (any !== undefined && check({ any })) ||
    (all !== undefined && check({ all }));

  return <>{ok ? children : fallback}</>;
}

/**
 * TanStack Router guard: ensures the session is loaded and the user has the required
 * permission(s). Throws a redirect to /login if unauthenticated, or a redirect to the
 * forbidden path (default `/`) if authenticated but lacking the permission.
 */
export async function ensurePermission(
  queryClient: QueryClient,
  required: PermissionCheck,
  options: { loginPath?: string; forbiddenPath?: string } = {},
): Promise<void> {
  const { loginPath = '/login', forbiddenPath = '/' } = options;

  let data;
  try {
    data = await queryClient.fetchQuery({
      ...getInfoQueryOptions(),
      queryFn: ({ signal }) => info({ signal }),
    });
  } catch {
    throw redirect({ to: loginPath });
  }

  const payload = data?.data as UserInfoResponse | undefined;
  const granted = new Set<string>(payload?.permissions ?? []);
  if (!evaluate(granted, required)) {
    throw redirect({ to: forbiddenPath });
  }
}
