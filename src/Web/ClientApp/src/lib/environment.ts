import { useGetSystemInfo } from '@/api/generated/system-info/system-info';
import type { SystemInfoResponse } from '@/api/generated/model';

export type EnvironmentName = 'Development' | 'Staging' | 'Production' | 'Unknown';

/**
 * The current deployment environment as reported by the backend (`GET /api/SystemInfo`), used to
 * show a test-environment banner. Cached for the whole session — the environment cannot change
 * while the app is running.
 */
export function useEnvironment() {
  const { data } = useGetSystemInfo({
    query: {
      staleTime: Infinity,
      gcTime: Infinity,
      retry: false,
      refetchOnWindowFocus: false,
      refetchOnReconnect: false,
    },
  });

  const body = data?.data as SystemInfoResponse | undefined;
  const environment = (body?.environment ?? 'Unknown') as EnvironmentName;
  const isProduction = environment === 'Production';
  const isStaging = environment === 'Staging';
  const isDevelopment = environment === 'Development';
  const isTestEnv = isStaging || isDevelopment;

  return { environment, isProduction, isStaging, isDevelopment, isTestEnv };
}
