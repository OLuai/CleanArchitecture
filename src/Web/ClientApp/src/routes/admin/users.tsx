import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import type { ColumnDef, SortingState } from '@tanstack/react-table';
import { MoreHorizontal, Plus, Search } from 'lucide-react';
import { toast } from 'sonner';

import { DataTable } from '@/components/data-table';
import { PageHeader } from '@/components/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { UserFormDialog } from '@/components/admin/user-form-dialog';
import { UserRolesDialog } from '@/components/admin/user-roles-dialog';
import { UserPasswordDialog } from '@/components/admin/user-password-dialog';
import { ConfirmDialog } from '@/components/confirm-dialog';
import { useGetRoles } from '@/api/generated/roles/roles';
import {
  getGetUsersQueryKey,
  useDeleteUser,
  useGetUsers,
  useSetUserActive,
} from '@/api/generated/users/users';
import type { UserDto } from '@/api/generated/model';
import { successData } from '@/lib/api';
import { apiErrorMessage } from '@/lib/api-error';
import { Can, Permission, ensurePermission, usePermissions } from '@/lib/permissions';
import { useDebounced } from '@/lib/use-debounced';

const PAGE_SIZE = 20;
const ALL_ROLES = '__all__';

export const Route = createFileRoute('/admin/users')({
  beforeLoad: ({ context }) => ensurePermission(context.queryClient, Permission.UsersView),
  component: UsersPage,
});

function UsersPage() {
  const queryClient = useQueryClient();
  const { has } = usePermissions();

  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState(ALL_ROLES);
  const [pageNumber, setPageNumber] = useState(1);
  const [sorting, setSorting] = useState<SortingState>([]);

  const debouncedSearch = useDebounced(search);

  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<UserDto | null>(null);
  const [assigningRoles, setAssigningRoles] = useState<UserDto | null>(null);
  const [resettingPassword, setResettingPassword] = useState<UserDto | null>(null);
  const [deleting, setDeleting] = useState<UserDto | null>(null);

  const sort = sorting[0];

  const users = useGetUsers({
    Search: debouncedSearch || undefined,
    Role: roleFilter === ALL_ROLES ? undefined : roleFilter,
    PageNumber: pageNumber,
    PageSize: PAGE_SIZE,
    SortBy: sort?.id,
    SortDescending: sort?.desc,
  });

  const roles = useGetRoles({ query: { enabled: has(Permission.RolesView) } });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: getGetUsersQueryKey() });

  const setActive = useSetUserActive();
  const deleteUser = useDeleteUser();

  const page = successData(users.data);
  const roleList = successData(roles.data) ?? [];

  const toggleActive = async (user: UserDto) => {
    try {
      await setActive.mutateAsync({ id: user.id, data: { isActive: !user.isActive } });
      toast.success(user.isActive ? 'User deactivated.' : 'User activated.');
      await invalidate();
    } catch (e) {
      toast.error(apiErrorMessage(e, 'Could not change the account status.'));
    }
  };

  const confirmDelete = async () => {
    if (!deleting) return;
    try {
      await deleteUser.mutateAsync({ id: deleting.id });
      toast.success('User deleted.');
      setDeleting(null);
      await invalidate();
    } catch (e) {
      toast.error(apiErrorMessage(e, 'Could not delete the user.'));
    }
  };

  const columns: ColumnDef<UserDto>[] = [
    {
      accessorKey: 'userName',
      header: 'User',
      cell: ({ row }) => (
        <div className="min-w-0">
          <div className="truncate font-medium">
            {row.original.displayName || row.original.userName}
          </div>
          {row.original.displayName && (
            <div className="text-muted-foreground truncate text-xs">{row.original.userName}</div>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'email',
      header: 'Email',
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.original.email ?? '—'}</span>
      ),
    },
    {
      id: 'roles',
      header: 'Roles',
      enableSorting: false,
      cell: ({ row }) =>
        row.original.roles.length === 0 ? (
          <span className="text-muted-foreground">—</span>
        ) : (
          <div className="flex flex-wrap gap-1">
            {row.original.roles.map(role => (
              <Badge key={role} variant="secondary">
                {role}
              </Badge>
            ))}
          </div>
        ),
    },
    {
      accessorKey: 'lockoutEnd',
      header: 'Status',
      cell: ({ row }) =>
        row.original.isActive ? (
          <Badge variant="outline">Active</Badge>
        ) : (
          <Badge variant="destructive">Deactivated</Badge>
        ),
    },
    {
      id: 'actions',
      header: '',
      enableSorting: false,
      cell: ({ row }) => {
        const user = row.original;
        const canUpdate = has(Permission.UsersUpdate);
        const canManageRoles = has(Permission.UsersManageRoles);
        const canDelete = has(Permission.UsersDelete);

        if (!canUpdate && !canManageRoles && !canDelete) return null;

        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" aria-label={`Actions for ${user.userName}`}>
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {canUpdate && (
                <DropdownMenuItem onSelect={() => setEditing(user)}>Edit</DropdownMenuItem>
              )}
              {canManageRoles && (
                <DropdownMenuItem onSelect={() => setAssigningRoles(user)}>
                  Manage roles
                </DropdownMenuItem>
              )}
              {canUpdate && (
                <DropdownMenuItem onSelect={() => setResettingPassword(user)}>
                  Reset password
                </DropdownMenuItem>
              )}
              {canUpdate && (
                <DropdownMenuItem onSelect={() => toggleActive(user)}>
                  {user.isActive ? 'Deactivate' : 'Activate'}
                </DropdownMenuItem>
              )}
              {canDelete && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem variant="destructive" onSelect={() => setDeleting(user)}>
                    Delete
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        );
      },
    },
  ];

  return (
    <>
      <PageHeader title="Users" description="Accounts, their roles and their status.">
        <Can permission={Permission.UsersCreate}>
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            New user
          </Button>
        </Can>
      </PageHeader>

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <div className="relative min-w-56 flex-1">
          <Search className="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
          <Input
            className="pl-8"
            placeholder="Search by name or email"
            value={search}
            onChange={e => {
              // A filter change invalidates the current page number: page 4 of the old result
              // set is unlikely to exist in the new one.
              setPageNumber(1);
              setSearch(e.target.value);
            }}
          />
        </div>

        {roleList.length > 0 && (
          <Select
            value={roleFilter}
            onValueChange={value => {
              setPageNumber(1);
              setRoleFilter(value);
            }}
          >
            <SelectTrigger className="w-48">
              <SelectValue placeholder="All roles" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL_ROLES}>All roles</SelectItem>
              {roleList.map(role => (
                <SelectItem key={role.id} value={role.name}>
                  {role.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </div>

      <DataTable
        columns={columns}
        data={page?.items ?? []}
        isLoading={users.isLoading}
        emptyMessage="No users match these filters."
        sorting={sorting}
        onSortingChange={updater => {
          setPageNumber(1);
          setSorting(updater);
        }}
        pagination={{
          pageNumber: page?.pageNumber ?? 1,
          pageSize: page?.pageSize ?? PAGE_SIZE,
          totalCount: page?.totalCount ?? 0,
          totalPages: page?.totalPages ?? 0,
          onPageChange: setPageNumber,
        }}
      />

      <UserFormDialog
        open={creating}
        onOpenChange={setCreating}
        roles={roleList}
        onSaved={invalidate}
      />

      <UserFormDialog
        key={editing?.id}
        user={editing ?? undefined}
        open={editing !== null}
        onOpenChange={open => !open && setEditing(null)}
        roles={roleList}
        onSaved={invalidate}
      />

      <UserRolesDialog
        key={`roles-${assigningRoles?.id}`}
        user={assigningRoles}
        open={assigningRoles !== null}
        onOpenChange={open => !open && setAssigningRoles(null)}
        roles={roleList}
        onSaved={invalidate}
      />

      <UserPasswordDialog
        key={`password-${resettingPassword?.id}`}
        user={resettingPassword}
        open={resettingPassword !== null}
        onOpenChange={open => !open && setResettingPassword(null)}
      />

      <ConfirmDialog
        open={deleting !== null}
        onOpenChange={open => !open && setDeleting(null)}
        title="Delete this user?"
        description={`${deleting?.userName ?? ''} will be removed permanently. Deactivate instead if you only want to block access.`}
        confirmLabel="Delete"
        destructive
        onConfirm={confirmDelete}
      />
    </>
  );
}
