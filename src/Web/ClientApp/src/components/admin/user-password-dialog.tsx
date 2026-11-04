import { useState } from 'react';
import { useForm } from '@tanstack/react-form';
import { toast } from 'sonner';

import { FieldError } from '@/components/field-error';
import { FormErrorSummary } from '@/components/form-error-summary';
import { Button } from '@/components/ui/button';
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
import { useSetUserPassword } from '@/api/generated/users/users';
import type { UserDto } from '@/api/generated/model';
import { applyServerErrors } from '@/lib/form-server-errors';

export function UserPasswordDialog({
  user,
  open,
  onOpenChange,
}: {
  user: UserDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const [generalErrors, setGeneralErrors] = useState<string[]>([]);
  const setPassword = useSetUserPassword();

  const form = useForm({
    defaultValues: { newPassword: '' },
    onSubmit: async ({ value }) => {
      if (!user) return;
      setGeneralErrors([]);
      try {
        await setPassword.mutateAsync({ id: user.id, data: { newPassword: value.newPassword } });
        toast.success('Password reset.');
        onOpenChange(false);
      } catch (e) {
        setGeneralErrors(applyServerErrors(form, e, 'Could not reset the password.'));
      }
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Reset password</DialogTitle>
          <DialogDescription>
            Sets a new password for {user?.userName} without needing the current one.
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

          <form.Field name="newPassword">
            {field => (
              <div className="space-y-2">
                <Label htmlFor="newPassword">New password</Label>
                <Input
                  id="newPassword"
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

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <form.Subscribe selector={s => s.isSubmitting}>
              {isSubmitting => (
                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Saving…' : 'Reset'}
                </Button>
              )}
            </form.Subscribe>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
