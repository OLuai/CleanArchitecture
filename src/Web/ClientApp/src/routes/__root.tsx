import { createRootRouteWithContext, Outlet } from '@tanstack/react-router';
import type { QueryClient } from '@tanstack/react-query';

import { NavMenu } from '@/components/nav-menu';
import { Toaster } from '@/components/ui/sonner';

export interface RouterContext {
  queryClient: QueryClient;
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
});

function RootLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <NavMenu />
      <main className="container mx-auto flex-1 px-4 py-6">
        <Outlet />
      </main>
      <Toaster richColors closeButton />
    </div>
  );
}
