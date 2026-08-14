import {
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnDef,
  type OnChangeFn,
  type SortingState,
} from '@tanstack/react-table';
import { useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';

export interface PaginationState {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (pageNumber: number) => void;
}

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  isLoading?: boolean;
  emptyMessage?: string;
  /**
   * Server-driven sorting. When provided, the table stops sorting rows itself and reports
   * changes instead — the page only holds one slice of the data, so sorting it locally would
   * reorder that slice rather than the whole set.
   */
  sorting?: SortingState;
  onSortingChange?: OnChangeFn<SortingState>;
  /** Server-driven pagination. Omit for a plain, unpaginated table. */
  pagination?: PaginationState;
}

export function DataTable<TData, TValue>({
  columns,
  data,
  isLoading = false,
  emptyMessage = 'No results.',
  sorting: controlledSorting,
  onSortingChange,
  pagination,
}: DataTableProps<TData, TValue>) {
  const [localSorting, setLocalSorting] = useState<SortingState>([]);
  const isServerSorted = controlledSorting !== undefined;

  const table = useReactTable({
    data,
    columns,
    state: { sorting: controlledSorting ?? localSorting },
    onSortingChange: onSortingChange ?? setLocalSorting,
    manualSorting: isServerSorted,
    getCoreRowModel: getCoreRowModel(),
    ...(isServerSorted ? {} : { getSortedRowModel: getSortedRowModel() }),
  });

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map(headerGroup => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map(header => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }, (_, rowIndex) => (
                <TableRow key={rowIndex}>
                  {columns.map((_, cellIndex) => (
                    <TableCell key={cellIndex}>
                      <Skeleton className="h-5 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : table.getRowModel().rows.length ? (
              table.getRowModel().rows.map(row => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map(cell => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center">
                  {emptyMessage}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {pagination && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-muted-foreground text-sm">
            Page {pagination.pageNumber} of {pagination.totalPages} · {pagination.totalCount} total
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.pageNumber <= 1}
              onClick={() => pagination.onPageChange(pagination.pageNumber - 1)}
            >
              <ChevronLeft className="size-4" />
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.pageNumber >= pagination.totalPages}
              onClick={() => pagination.onPageChange(pagination.pageNumber + 1)}
            >
              Next
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
