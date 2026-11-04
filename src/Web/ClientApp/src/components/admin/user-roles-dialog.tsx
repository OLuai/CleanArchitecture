import { useState } from 'react';
import { toast } from 'sonner';

import { FormErrorSummary } from '@/components/form-error-summary';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useSetUserRoles } from '@/api/generated/users/users';
import type { RoleDto, UserDto } from '@/api/generated/model';
import { apiErrorMessage } from '@/lib/api-error';

export function UserRolesDialog({
  user,
  open,
  onOpenChange,
  roles,
  onSaved,
}: {
  user: UserDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  roles: RoleDto[];
  onSaved: () => void | Promise<unknown>;
}) {
  const [selected, setSelected] = useState<string[]>(user?.roles ?? []);
  const [error, setError] = useState<string[]>([]);
  const setUserRoles = useSetUserRoles();

  const submit = async () => {
    if (!user) return;
    setError([]);
    try {
      await setUserRoles.mutateAsync({ id: user.id, data: { roles: selected } });
      toast.success('Roles updated. The change reaches open sessions within a few minutes.');
      await onSaved();
      onOpenChange(false);
    } catch (e) {
      setError([apiErrorMessage(e, 'Could not update the roles.')]);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Manage roles</DialogTitle>
          <DialogDescription>
            Permissions come from roles, so this is what decides what {user?.userName} can do.
          </DialogDescription>
        </DialogHeader>

        <FormErrorSummary messages={error} />

        <div className="space-y-3">
          {roles.length === 0 && <p className="text-muted-foreground text-sm">No roles defined.</p>}
          {roles.map(role => (
            <label key={role.id} className="flex items-start gap-2 text-sm">
              <Checkbox
                className="mt-0.5"
                checked={selected.includes(role.name)}
                onCheckedChange={checked =>
                  setSelected(current =>
                    checked ? [...current, role.name] : current.filter(r => r !== role.name),
                  )
                }
              />
              <span>
                <span className="font-medium">{role.name}</span>
                <span className="text-muted-foreground block text-xs">
                  {role.permissions.length} permission{role.permissions.length === 1 ? '' : 's'}
                </span>
              </span>
            </label>
          ))}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={submit} disabled={setUserRoles.isPending}>
            {setUserRoles.isPending ? 'Saving…' : 'Save'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
