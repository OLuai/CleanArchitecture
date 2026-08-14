import { WifiOff } from 'lucide-react';

import { useNetworkStatus } from '@/lib/network';

export function OfflineBanner() {
  const isOnline = useNetworkStatus();
  if (isOnline) return null;

  return (
    <div className="bg-destructive text-destructive-foreground fixed inset-x-0 top-0 z-50 flex items-center justify-center gap-2 px-4 py-2 text-sm font-medium">
      <WifiOff className="size-4" />
      Vous êtes hors ligne
    </div>
  );
}
