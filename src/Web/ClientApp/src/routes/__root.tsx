import {
  createRootRouteWithContext,
  Outlet,
  useRouterState,
} from '@tanstack/react-router';
import type { QueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';

import { AppSidebar } from '@/components/app-sidebar';
import { EnvironmentBanner } from '@/components/environment-banner';
import { OfflineBanner } from '@/components/offline-banner';
import { PageBreadcrumb } from '@/components/page-breadcrumb';
import { Separator } from '@/components/ui/separator';
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from '@/components/ui/sidebar';
import { Toaster } from '@/components/ui/sonner';
import { getInfoQueryOptions, info } from '@/api/generated/users/users';

export interface RouterContext {
  queryClient: QueryClient;
}

/** Routes rendered without the dashboard shell. */
const BARE_ROUTES = ['/login', '/register'];

export const Route = createRootRouteWithContext<RouterContext>()({
  // Resolve the session once, before the first render. Without this the shell mounts with an
  // empty permission set and the sidebar visibly fills in a moment later. A rejected probe just
  // means "not signed in", which the individual route guards handle.
  beforeLoad: async ({ context }) => {
    await context.queryClient
      .fetchQuery({
        ...getInfoQueryOptions(),
        queryFn: ({ signal }) => info({ signal }),
      })
      .catch(() => {});
  },
  pendingMs: 0,
  pendingComponent: SessionLoader,
  component: RootLayout,
});

function SessionLoader() {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <Loader2 className="text-muted-foreground size-6 animate-spin" />
      <span className="sr-only">Loading</span>
    </div>
  );
}

function RootLayout() {
  const pathname = useRouterState({ select: state => state.location.pathname });
  const isBare = BARE_ROUTES.includes(pathname);

  if (isBare) {
    return (
      <div className="flex min-h-screen flex-col">
        <EnvironmentBanner />
        <OfflineBanner />
        <main className="flex flex-1 items-center justify-center px-4 py-10">
          <Outlet />
        </main>
        <Toaster richColors closeButton />
      </div>
    );
  }

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        <EnvironmentBanner />
        <OfflineBanner />
        <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1" />
          <Separator orientation="vertical" className="mr-2 h-4" />
          <PageBreadcrumb />
        </header>
        <main className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
        <Toaster richColors closeButton />
      </SidebarInset>
    </SidebarProvider>
  );
}
