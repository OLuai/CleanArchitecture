import { useQueryClient } from '@tanstack/react-query';
import {
  useInfo,
  useLogin as useLoginMutation,
  useRegister as useRegisterMutation,
  useLogout as useLogoutMutation,
  getInfoQueryKey,
  getInfoQueryOptions,
  info,
} from '@/api/generated/users/users';
import type { QueryClient } from '@tanstack/react-query';
import type { UserInfoResponse } from '@/api/generated/model';
import { successData } from '@/lib/api';

/**
 * Reactive session hook. The `info` endpoint returns 200 for an authenticated user and 401
 * otherwise (surfaced as an error by the fetch mutator), so success ⇔ authenticated.
 */
export function useSession() {
  const query = useInfo();
  return {
    user: successData(query.data) as UserInfoResponse | undefined,
    isAuthenticated: query.isSuccess,
    isLoading: query.isLoading,
    query,
  };
}

export function useLogin() {
  const queryClient = useQueryClient();
  return useLoginMutation({
    mutation: {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: getInfoQueryKey() }),
    },
  });
}

export function useRegister() {
  return useRegisterMutation();
}

export function useLogout() {
  const queryClient = useQueryClient();
  return useLogoutMutation({
    mutation: {
      onSuccess: () => queryClient.invalidateQueries({ queryKey: getInfoQueryKey() }),
    },
  });
}

/**
 * Used by route `beforeLoad` guards: resolves the current session, throwing if unauthenticated.
 */
export async function ensureAuthenticated(queryClient: QueryClient) {
  await queryClient.fetchQuery({
    ...getInfoQueryOptions(),
    queryFn: ({ signal }) => info({ signal }),
  });
}
