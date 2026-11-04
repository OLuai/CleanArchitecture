import { useState } from 'react';
import { useForm } from '@tanstack/react-form';
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
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useCreateUser, useUpdateUser } from '@/api/generated/users/users';
import type { RoleDto, UserDto } from '@/api/generated/model';
import { applyServerErrors } from '@/lib/form-server-errors';
import { FieldError } from '@/components/field-error';

/**
 * Creates a user, or edits one when `user` is provided. Roles can only be chosen at creation
 * time; afterwards they are managed through the dedicated roles dialog, which is gated by a
 * separate permission.
 */
export function UserFormDialog({
  user,
  open,
  onOpenChange,
  roles,
  onSaved,
}: {
  user?: UserDto;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  roles: RoleDto[];
  onSaved: () => void | Promise<unknown>;
}) {
  const isEdit = user !== undefined;
  const [generalErrors, setGeneralErrors] = useState<string[]>([]);

  const createUser = useCreateUser();
  const updateUser = useUpdateUser();

  const form = useForm({
    defaultValues: {
      userName: user?.userName ?? '',
      email: user?.email ?? '',
      displayName: user?.displayName ?? '',
      password: '',
      roles: user?.roles ?? ([] as string[]),
    },
    onSubmit: async ({ value }) => {
      setGeneralErrors([]);
      try {
        if (isEdit) {
          await updateUser.mutateAsync({
            id: user.id,
            data: {
              userName: value.userName,
              email: value.email,
              displayName: value.displayName || undefined,
            },
          });
          toast.success('User updated.');
        } else {
          await createUser.mutateAsync({
            data: {
              userName: value.userName,
              email: value.email,
              password: value.password,
              displayName: value.displayName || undefined,
              roles: value.roles,
            },
          });
          toast.success('User created.');
        }
        await onSaved();
        onOpenChange(false);
      } catch (e) {
        setGeneralErrors(applyServerErrors(form, e, 'Could not save the user.'));
      }
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit user' : 'New user'}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? 'Update the account details. Roles and password are managed separately.'
              : 'Create an account and optionally assign its roles.'}
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

          <form.Field name="userName">
            {field => (
              <div className="space-y-2">
                <Label htmlFor="userName">User name</Label>
                <Input
                  id="userName"
                  autoComplete="off"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={e => field.handleChange(e.target.value)}
                  aria-invalid={field.state.meta.errors.length > 0 || undefined}
                />
                <FieldError meta={field.state.meta} />
              </div>
            )}
          </form.Field>

          <form.Field name="email">
            {field => (
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  autoComplete="off"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={e => field.handleChange(e.target.value)}
                  aria-invalid={field.state.meta.errors.length > 0 || undefined}
                />
                <FieldError meta={field.state.meta} />
              </div>
            )}
          </form.Field>

          <form.Field name="displayName">
            {field => (
              <div className="space-y-2">
                <Label htmlFor="displayName">Display name</Label>
                <Input
                  id="displayName"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={e => field.handleChange(e.target.value)}
                />
                <FieldError meta={field.state.meta} />
              </div>
            )}
          </form.Field>

          {!isEdit && (
            <>
              <form.Field name="password">
                {field => (
                  <div className="space-y-2">
                    <Label htmlFor="password">Password</Label>
                    <Input
                      id="password"
                      type="password"
                      autoComplete="new-password"
                      value={field.state.value}
                      onBlur={field.handleBlur}
                      onChange={e => field.handleChange(e.target.value)}
                      aria-invalid={field.state.meta.errors.length > 0 || undefined}
                    />
                    <FieldError meta={field.state.meta} />
                  </div>
                )}
              </form.Field>

              {roles.length > 0 && (
                <form.Field name="roles">
                  {field => (
                    <div className="space-y-2">
                      <Label>Roles</Label>
                      <div className="space-y-2">
                        {roles.map(role => (
                          <label key={role.id} className="flex items-center gap-2 text-sm">
                            <Checkbox
                              checked={field.state.value.includes(role.name)}
                              onCheckedChange={checked =>
                                field.handleChange(
                                  checked
                                    ? [...field.state.value, role.name]
                                    : field.state.value.filter(r => r !== role.name),
                                )
                              }
                            />
                            {role.name}
                          </label>
                        ))}
                      </div>
                      <FieldError meta={field.state.meta} />
                    </div>
                  )}
                </form.Field>
              )}
            </>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <form.Subscribe selector={s => s.isSubmitting}>
              {isSubmitting => (
                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Saving…' : 'Save'}
                </Button>
              )}
            </form.Subscribe>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
