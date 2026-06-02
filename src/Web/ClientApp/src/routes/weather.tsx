import { createFileRoute, redirect } from '@tanstack/react-router';
import { type ColumnDef } from '@tanstack/react-table';
import { ArrowUpDownIcon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/data-table';
import { ensureAuthenticated } from '@/lib/auth';
import { useGetWeatherForecasts } from '@/api/generated/weather-forecasts/weather-forecasts';
import type { WeatherForecast } from '@/api/generated/model';

export const Route = createFileRoute('/weather')({
  beforeLoad: async ({ context, location }) => {
    try {
      await ensureAuthenticated(context.queryClient);
    } catch {
      throw redirect({ to: '/login', search: { returnUrl: location.pathname } });
    }
  },
  component: WeatherPage,
});

const columns: ColumnDef<WeatherForecast>[] = [
  {
    accessorKey: 'date',
    header: ({ column }) => (
      <Button
        variant="ghost"
        size="sm"
        className="-ml-3"
        onClick={() => column.toggleSorting(column.getIsSorted() === 'asc')}
      >
        Date
        <ArrowUpDownIcon className="ml-1 size-3.5" />
      </Button>
    ),
    cell: ({ row }) =>
      new Date(row.original.date as string).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      }),
  },
  { accessorKey: 'temperatureC', header: 'Temp. (C)' },
  { accessorKey: 'temperatureF', header: 'Temp. (F)' },
  { accessorKey: 'summary', header: 'Summary' },
];

function WeatherPage() {
  const { data, isLoading, isError } = useGetWeatherForecasts();
  // The mutator throws on non-2xx, so a resolved query is always the success payload.
  const forecasts = (data?.data ?? []) as WeatherForecast[];

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <div className="space-y-1">
        <h1 className="text-3xl font-bold tracking-tight">Weather</h1>
        <p className="text-muted-foreground">
          This component demonstrates fetching data from the server with TanStack Query and
          rendering it with TanStack Table.
        </p>
      </div>

      {isLoading && <p className="text-muted-foreground">Fetching your weather forecast…</p>}
      {isError && (
        <p className="text-destructive">
          Unable to load weather forecasts. Please try again later.
        </p>
      )}
      {!isLoading && !isError && <DataTable columns={columns} data={forecasts} />}
    </div>
  );
}
