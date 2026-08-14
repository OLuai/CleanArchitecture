import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { ConfirmDialog } from '@/components/confirm-dialog';
import { FormErrorSummary } from '@/components/form-error-summary';
import { PageHeader } from '@/components/page-header';
import { RoleFormDialog } from '@/components/admin/role-form-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Skeleton } from '@/components/ui/skeleton';
import {
  getGetRolesQueryKey,
  useDeleteRole,
  useGetPermissions,
  useGetRoles,
  useSetRolePermissions,
} from '@/api/generated/roles/roles';
import type { RoleDto } from '@/api/generated/model';
import { successData } from '@/lib/api';
import { apiErrorMessage } from '@/lib/api-error';
import { Can, Permission, ensurePermission, usePermissions } from '@/lib/permissions';

export const Route = createFileRoute('/admin/roles')({
  beforeLoad: ({ context }) => ensurePermission(context.queryClient, Permission.RolesView),
  component: RolesPage,
});

function RolesPage() {
  const queryClient = useQueryClient();
  const { has } = usePermissions();
  const canManage = has(Permission.RolesManage);

  const roles = useGetRoles();
  const permissions = useGetPermissions();

  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [deleting, setDeleting] = useState<RoleDto | null>(null);

  const roleList = successData(roles.data) ?? [];
  const permissionGroups = successData(permissions.data) ?? [];
  const selectedRole = roleList.find(r => r.id === selectedRoleId) ?? roleList[0];

  const invalidate = () => queryClient.invalidateQueries({ queryKey: getGetRolesQueryKey() });

  const deleteRole = useDeleteRole();

  const confirmDelete = async () => {
    if (!deleting) return;
    try {
      await deleteRole.mutateAsync({ id: deleting.id });
      toast.success('Role deleted.');
      setDeleting(null);
      if (selectedRoleId === deleting.id) setSelectedRoleId(null);
      await invalidate();
    } catch (e) {
      toast.error(apiErrorMessage(e, 'Could not delete the role.'));
    }
  };

  return (
    <>
      <PageHeader
        title="Roles"
        description="Permissions are granted to roles; users inherit them from the roles they hold."
      >
        <Can permission={Permission.RolesManage}>
          <Button onClick={() => setCreating(true)}>
            <Plus className="size-4" />
            New role
          </Button>
        </Can>
      </PageHeader>

      <div className="grid gap-4 lg:grid-cols-[18rem_1fr]">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Roles</CardTitle>
          </CardHeader>
          <CardContent className="space-y-1">
            {roles.isLoading &&
              Array.from({ length: 3 }, (_, i) => <Skeleton key={i} className="h-10 w-full" />)}

            {roleList.map(role => (
              <button
                key={role.id}
                type="button"
                onClick={() => setSelectedRoleId(role.id)}
                className={`hover:bg-accent flex w-full items-center justify-between rounded-md px-3 py-2 text-left text-sm ${
                  selectedRole?.id === role.id ? 'bg-accent' : ''
                }`}
              >
                <span className="min-w-0">
                  <span className="block truncate font-medium">{role.name}</span>
                  <span className="text-muted-foreground block text-xs">
                    {role.userCount} user{role.userCount === 1 ? '' : 's'}
                  </span>
                </span>
                {role.isBuiltIn && <Badge variant="outline">Built-in</Badge>}
              </button>
            ))}
          </CardContent>
        </Card>

        {selectedRole && (
          <PermissionMatrix
            key={selectedRole.id}
            role={selectedRole}
            groups={permissionGroups}
            isLoading={permissions.isLoading}
            canManage={canManage}
            onSaved={invalidate}
            onDelete={() => setDeleting(selectedRole)}
          />
        )}
      </div>

      <RoleFormDialog
        open={creating}
        onOpenChange={setCreating}
        groups={permissionGroups}
        onSaved={invalidate}
      />

      <ConfirmDialog
        open={deleting !== null}
        onOpenChange={open => !open && setDeleting(null)}
        title="Delete this role?"
        description={`${deleting?.name ?? ''} will be removed. Roles that are built in, or that still have members, cannot be deleted.`}
        confirmLabel="Delete"
        destructive
        onConfirm={confirmDelete}
      />
    </>
  );
}

function PermissionMatrix({
  role,
  groups,
  isLoading,
  canManage,
  onSaved,
  onDelete,
}: {
  role: RoleDto;
  groups: { area: string; permissions: string[] }[];
  isLoading: boolean;
  canManage: boolean;
  onSaved: () => void | Promise<unknown>;
  onDelete: () => void;
}) {
  const [selected, setSelected] = useState<string[]>(role.permissions);
  const [errors, setErrors] = useState<string[]>([]);
  const setRolePermissions = useSetRolePermissions();

  const isDirty =
    selected.length !== role.permissions.length ||
    selected.some(p => !role.permissions.includes(p));

  const toggle = (permission: string, checked: boolean) =>
    setSelected(current =>
      checked ? [...current, permission] : current.filter(p => p !== permission),
    );

  const save = async () => {
    setErrors([]);
    try {
      await setRolePermissions.mutateAsync({ id: role.id, data: { permissions: selected } });
      toast.success('Permissions updated. Members pick up the change within a few minutes.');
      await onSaved();
    } catch (e) {
      setErrors([apiErrorMessage(e, 'Could not update the permissions.')]);
    }
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
        <div>
          <CardTitle className="text-base">{role.name}</CardTitle>
          <p className="text-muted-foreground mt-1 text-sm">
            {role.permissions.length} permission{role.permissions.length === 1 ? '' : 's'} ·{' '}
            {role.userCount} user{role.userCount === 1 ? '' : 's'}
          </p>
        </div>
        {canManage && !role.isBuiltIn && (
          <Button variant="ghost" size="icon" onClick={onDelete} aria-label={`Delete ${role.name}`}>
            <Trash2 className="size-4" />
          </Button>
        )}
      </CardHeader>

      <CardContent className="space-y-4">
        <FormErrorSummary messages={errors} />

        {isLoading &&
          Array.from({ length: 3 }, (_, i) => <Skeleton key={i} className="h-20 w-full" />)}

        {groups.map(group => (
          <div key={group.area}>
            <h3 className="mb-2 text-sm font-medium">{group.area}</h3>
            <div className="grid gap-2 sm:grid-cols-2">
              {group.permissions.map(permission => (
                <label key={permission} className="flex items-center gap-2 text-sm">
                  <Checkbox
                    checked={selected.includes(permission)}
                    disabled={!canManage}
                    onCheckedChange={checked => toggle(permission, checked === true)}
                  />
                  <span className="text-muted-foreground">
                    {permission.slice(permission.indexOf('.') + 1)}
                  </span>
                </label>
              ))}
            </div>
          </div>
        ))}

        {canManage && (
          <div className="flex items-center gap-2 pt-2">
            <Button onClick={save} disabled={!isDirty || setRolePermissions.isPending}>
              {setRolePermissions.isPending ? 'Saving…' : 'Save permissions'}
            </Button>
            {isDirty && (
              <Button variant="ghost" onClick={() => setSelected(role.permissions)}>
                Reset
              </Button>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
