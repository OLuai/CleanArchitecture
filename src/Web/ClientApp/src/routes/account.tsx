import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import { useForm } from '@tanstack/react-form';
import { toast } from 'sonner';

import { FieldError } from '@/components/field-error';
import { FormErrorSummary } from '@/components/form-error-summary';
import { PageHeader } from '@/components/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  getInfoQueryKey,
  usePostApiUsersIdentityManageInfo,
} from '@/api/generated/users/users';
import { ensureAuthenticated, useSession } from '@/lib/auth';
import { applyServerErrors } from '@/lib/form-server-errors';

export const Route = createFileRoute('/account')({
  beforeLoad: ({ context }) => ensureAuthenticated(context.queryClient),
  component: AccountPage,
});

function AccountPage() {
  const { user } = useSession();

  return (
    <>
      <PageHeader title="My account" description="Your profile, credentials and permissions." />

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Profile</CardTitle>
            <CardDescription>
              An administrator can change your name and roles from the Users screen.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3 text-sm">
            <Row label="User name" value={user?.userName ?? '—'} />
            <Row label="Display name" value={user?.displayName || '—'} />
            <Row label="Email" value={user?.email ?? '—'} />
            <div>
              <p className="text-muted-foreground mb-1">Roles</p>
              <div className="flex flex-wrap gap-1">
                {user?.roles?.length ? (
                  user.roles.map(role => (
                    <Badge key={role} variant="secondary">
                      {role}
                    </Badge>
                  ))
                ) : (
                  <span>—</span>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

        <ChangeEmailCard currentEmail={user?.email ?? ''} />
        <ChangePasswordCard />
        <PermissionsCard permissions={user?.permissions ?? []} />
      </div>
    </>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-muted-foreground">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  );
}

function ChangeEmailCard({ currentEmail }: { currentEmail: string }) {
  const queryClient = useQueryClient();
  const [generalErrors, setGeneralErrors] = useState<string[]>([]);
  const manageInfo = usePostApiUsersIdentityManageInfo();

  const form = useForm({
    defaultValues: { newEmail: currentEmail },
    onSubmit: async ({ value }) => {
      setGeneralErrors([]);
      try {
        await manageInfo.mutateAsync({ data: { newEmail: value.newEmail } });
        // Identity sends a confirmation link rather than switching the address immediately.
        toast.success('Check your inbox to confirm the new address.');
        await queryClient.invalidateQueries({ queryKey: getInfoQueryKey() });
      } catch (e) {
        setGeneralErrors(applyServerErrors(form, e, 'Could not change the email address.'));
      }
    },
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Email address</CardTitle>
        <CardDescription>A confirmation link is sent to the new address.</CardDescription>
      </CardHeader>
      <CardContent>
        <form
          className="space-y-4"
          onSubmit={e => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <FormErrorSummary messages={generalErrors} />

          <form.Field name="newEmail">
            {field => (
              <div className="space-y-2">
                <Label htmlFor="newEmail">New email</Label>
                <Input
                  id="newEmail"
                  type="email"
                  autoComplete="email"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={e => field.handleChange(e.target.value)}
                />
                <FieldError meta={field.state.meta} />
              </div>
            )}
          </form.Field>

          <form.Subscribe selector={s => [s.isSubmitting, s.values.newEmail] as const}>
            {([isSubmitting, newEmail]) => (
              <Button type="submit" disabled={isSubmitting || newEmail === currentEmail}>
                {isSubmitting ? 'Saving…' : 'Change email'}
              </Button>
            )}
          </form.Subscribe>
        </form>
      </CardContent>
    </Card>
  );
}

function ChangePasswordCard() {
  const [generalErrors, setGeneralErrors] = useState<string[]>([]);
  const manageInfo = usePostApiUsersIdentityManageInfo();

  const form = useForm({
    defaultValues: { oldPassword: '', newPassword: '' },
    onSubmit: async ({ value }) => {
      setGeneralErrors([]);
      try {
        await manageInfo.mutateAsync({
          data: { oldPassword: value.oldPassword, newPassword: value.newPassword },
        });
        toast.success('Password changed.');
        form.reset();
      } catch (e) {
        setGeneralErrors(applyServerErrors(form, e, 'Could not change the password.'));
      }
    },
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Password</CardTitle>
        <CardDescription>Changing your own password requires the current one.</CardDescription>
      </CardHeader>
      <CardContent>
        <form
          className="space-y-4"
          onSubmit={e => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <FormErrorSummary messages={generalErrors} />

          <form.Field name="oldPassword">
            {field => (
              <div className="space-y-2">
                <Label htmlFor="oldPassword">Current password</Label>
                <Input
                  id="oldPassword"
                  type="password"
                  autoComplete="current-password"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={e => field.handleChange(e.target.value)}
                />
                <FieldError meta={field.state.meta} />
              </div>
            )}
          </form.Field>

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
                />
                <FieldError meta={field.state.meta} />
              </div>
            )}
          </form.Field>

          <form.Subscribe selector={s => s.isSubmitting}>
            {isSubmitting => (
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Saving…' : 'Change password'}
              </Button>
            )}
          </form.Subscribe>
        </form>
      </CardContent>
    </Card>
  );
}

function PermissionsCard({ permissions }: { permissions: string[] }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Permissions</CardTitle>
        <CardDescription>
          Inherited from your roles. Ask an administrator to change them.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {permissions.length === 0 ? (
          <p className="text-muted-foreground text-sm">No permissions granted.</p>
        ) : (
          <div className="flex flex-wrap gap-1">
            {permissions.map(permission => (
              <Badge key={permission} variant="outline" className="font-mono text-xs">
                {permission}
              </Badge>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
