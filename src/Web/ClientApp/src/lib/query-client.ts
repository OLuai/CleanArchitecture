import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Auth probing (the `info` query) and most reads should not retry on a 401/4xx.
      retry: false,
      refetchOnWindowFocus: false,
      staleTime: 30_000,
    },
  },
});
