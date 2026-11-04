import { useState } from 'react';
import { useForm } from '@tanstack/react-form';
import { toast } from 'sonner';

import { FieldError } from '@/components/field-error';
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
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useCreateRole } from '@/api/generated/roles/roles';
import { applyServerErrors } from '@/lib/form-server-errors';

export function RoleFormDialog({
  open,
  onOpenChange,
  groups,
  onSaved,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  groups: { area: string; permissions: string[] }[];
  onSaved: () => void | Promise<unknown>;
}) {
  const [generalErrors, setGeneralErrors] = useState<string[]>([]);
  const createRole = useCreateRole();

  const form = useForm({
    defaultValues: { name: '', permissions: [] as string[] },
    onSubmit: async ({ value }) => {
      setGeneralErrors([]);
      try {
        await createRole.mutateAsync({ data: value });
        toast.success('Role created.');
        await onSaved();
        form.reset();
        onOpenChange(false);
      } catch (e) {
        setGeneralErrors(applyServerErrors(form, e, 'Could not create the role.'));
      }
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>New role</DialogTitle>
          <DialogDescription>
            Name the role and pick the permissions it grants. Both can be changed later.
          </DialogDescription>
        </DialogHeader>

        <form
          className="space-y-4"
          onSubmit={e => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <FormErrorSummary messages={generalErrors} />

          <form.Field name="name">
            {field => (
              <div className="space-y-2">
                <Label htmlFor="roleName">Name</Label>
                <Input
                  id="roleName"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={e => field.handleChange(e.target.value)}
                  aria-invalid={field.state.meta.errors.length > 0 || undefined}
                />
                <FieldError meta={field.state.meta} />
              </div>
            )}
          </form.Field>

          <form.Field name="permissions">
            {field => (
              <div className="max-h-72 space-y-4 overflow-y-auto pr-1">
                {groups.map(group => (
                  <div key={group.area}>
                    <h3 className="mb-2 text-sm font-medium">{group.area}</h3>
                    <div className="grid gap-2 sm:grid-cols-2">
                      {group.permissions.map(permission => (
                        <label key={permission} className="flex items-center gap-2 text-sm">
                          <Checkbox
                            checked={field.state.value.includes(permission)}
                            onCheckedChange={checked =>
                              field.handleChange(
                                checked === true
                                  ? [...field.state.value, permission]
                                  : field.state.value.filter(p => p !== permission),
                              )
                            }
                          />
                          <span className="text-muted-foreground">
                            {permission.slice(permission.indexOf('.') + 1)}
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </form.Field>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <form.Subscribe selector={s => s.isSubmitting}>
              {isSubmitting => (
                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Creating…' : 'Create'}
                </Button>
              )}
            </form.Subscribe>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
