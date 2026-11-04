import { useState } from 'react';
import { createFileRoute, redirect } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import { CheckCircle2Icon, SendIcon, XCircleIcon } from 'lucide-react';
import { toast } from 'sonner';

import { PageHeader } from '@/components/page-header';
import { ConfirmDialog } from '@/components/confirm-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import {
  getGetEmailSettingsQueryKey,
  useGetEmailSettings,
  useResetEmailSettings,
  useSaveEmailSettings,
  useSendTestEmail,
} from '@/api/generated/settings/settings';
import { EmailSettingsSource, SmtpSecurity, type EmailSettingsDto } from '@/api/generated/model';
import { successData } from '@/lib/api';
import { apiErrorMessage } from '@/lib/api-error';
import { ensureAuthenticated } from '@/lib/auth';
import { ensurePermission, Permission } from '@/lib/permissions';

export const Route = createFileRoute('/admin/email')({
  beforeLoad: async ({ context, location }) => {
    try {
      await ensureAuthenticated(context.queryClient);
    } catch {
      throw redirect({ to: '/login', search: { returnUrl: location.pathname } });
    }
    await ensurePermission(context.queryClient, Permission.SettingsManageEmail);
  },
  component: EmailSettingsPage,
});

const SOURCE_LABEL: Record<EmailSettingsSource, string> = {
  None: 'No relay configured',
  Environment: 'Environment settings',
  Database: 'Settings saved here',
};

/**
 * 587 is submission with STARTTLS, 465 is TLS from the first byte. "None" exists for a relay
 * reachable only from the private network — it is offered explicitly, and never negotiated, so
 * nobody ends up sending password-reset links in the clear without having chosen to.
 */
const ENCRYPTION: { value: SmtpSecurity; label: string; port: number }[] = [
  { value: SmtpSecurity.StartTls, label: 'STARTTLS (port 587)', port: 587 },
  { value: SmtpSecurity.Tls, label: 'TLS direct (port 465)', port: 465 },
  { value: SmtpSecurity.None, label: 'No encryption (internal relay)', port: 25 },
];

function EmailSettingsPage() {
  const query = useGetEmailSettings();
  const settings = successData(query.data);

  if (query.isLoading) {
    return (
      <>
        <PageHeader title="Email delivery" description="The relay that delivers outgoing messages." />
        <Skeleton className="h-[36rem] w-full max-w-2xl rounded-xl" />
      </>
    );
  }

  // Keyed on what is stored so a save or a reset reseeds the fields rather than leaving the
  // previous values on screen.
  return <EmailSettingsForm key={JSON.stringify(settings ?? null)} settings={settings} />;
}

function EmailSettingsForm({ settings }: { settings?: EmailSettingsDto }) {
  const queryClient = useQueryClient();
  const saveSettings = useSaveEmailSettings();
  const resetSettings = useResetEmailSettings();
  const sendTest = useSendTestEmail();

  const source = settings?.source ?? EmailSettingsSource.None;
  const isFromEnvironment = source === EmailSettingsSource.Environment;

  const [host, setHost] = useState(settings?.host ?? '');
  const [port, setPort] = useState(settings?.port ?? 587);
  const [security, setSecurity] = useState<SmtpSecurity>(settings?.security ?? SmtpSecurity.StartTls);
  const [userName, setUserName] = useState(settings?.userName ?? '');
  const [password, setPassword] = useState('');
  const [fromAddress, setFromAddress] = useState(settings?.fromAddress ?? '');
  const [fromName, setFromName] = useState(settings?.fromName ?? 'CleanArchitecture');
  const [confirmingReset, setConfirmingReset] = useState(false);
  const [testResult, setTestResult] = useState<{ ok: boolean; message: string } | null>(null);

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: getGetEmailSettingsQueryKey() });

  const save = async () => {
    setTestResult(null);
    try {
      await saveSettings.mutateAsync({
        data: {
          host: host.trim(),
          port,
          security,
          userName: userName.trim() || undefined,
          // Left out when untouched, so saving the form does not wipe a stored password the
          // screen never received and therefore cannot re-send.
          password: password === '' ? undefined : password,
          fromAddress: fromAddress.trim(),
          fromName: fromName.trim() || undefined,
        },
      });
      setPassword('');
      await invalidate();
      toast.success('Settings saved.');
    } catch (e) {
      toast.error(apiErrorMessage(e, 'Settings could not be saved.'));
    }
  };

  const reset = async () => {
    try {
      await resetSettings.mutateAsync();
      await invalidate();
      setConfirmingReset(false);
      toast.success('Settings removed. The environment settings apply again.');
    } catch (e) {
      toast.error(apiErrorMessage(e, 'Settings could not be removed.'));
    }
  };

  const test = async () => {
    setTestResult(null);
    try {
      const response = await sendTest.mutateAsync();
      const result = successData(response);
      setTestResult(
        result?.sent
          ? { ok: true, message: `Message sent to ${result.sentTo}. Check your inbox.` }
          : { ok: false, message: result?.error ?? 'The relay gave no details.' },
      );
    } catch (e) {
      setTestResult({ ok: false, message: apiErrorMessage(e, 'The test failed.') });
    }
  };

  return (
    <>
      <PageHeader title="Email delivery" description="The relay that delivers outgoing messages.">
        <Badge variant={source === EmailSettingsSource.None ? 'destructive' : 'outline'}>
          {SOURCE_LABEL[source]}
        </Badge>
      </PageHeader>

      <div className="max-w-2xl space-y-6">
        {source === EmailSettingsSource.None && (
          <div className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
            No relay is configured: email confirmation and password reset messages are not being
            sent. Enter a server below, or provide the
            <code className="mx-1 font-mono text-xs">Smtp__*</code> variables at deployment.
          </div>
        )}

        {isFromEnvironment && (
          <div className="rounded-md border border-sky-200 bg-sky-50 px-4 py-3 text-sm text-sky-900">
            These values come from the deployment configuration. Saving them here creates an
            override that takes precedence, even if the variables change later.
          </div>
        )}

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Server</CardTitle>
            <CardDescription>
              The SMTP relay and how to connect to it.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-[1fr_10rem]">
              <div className="space-y-2">
                <Label htmlFor="host">Server</Label>
                <Input
                  id="host"
                  value={host}
                  onChange={e => setHost(e.target.value)}
                  placeholder="smtp.example.com"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="port">Port</Label>
                <Input
                  id="port"
                  type="number"
                  min={1}
                  max={65535}
                  value={port}
                  onChange={e => setPort(e.target.valueAsNumber || 0)}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="encryption">Encryption</Label>
              <Select
                value={security}
                onValueChange={value => {
                  const chosen = ENCRYPTION.find(o => o.value === value) ?? ENCRYPTION[0];
                  setSecurity(chosen.value);
                  // The port almost always follows the choice; still editable afterwards.
                  setPort(chosen.port);
                }}
              >
                <SelectTrigger id="encryption" className="w-full sm:w-72">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ENCRYPTION.map(option => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-muted-foreground text-xs">
                {security === SmtpSecurity.None
                  ? 'Credentials and messages will travel in clear text. Only choose this for a relay reachable solely from the private network.'
                  : 'An invalid certificate makes the connection fail, since sending credentials pass through it.'}
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Authentication</CardTitle>
            <CardDescription>
              Leave empty for an internal relay that does not require it.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="userName">Username</Label>
              <Input
                id="userName"
                autoComplete="off"
                value={userName}
                onChange={e => setUserName(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="new-password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder={settings?.hasPassword ? 'Saved — leave empty to keep it' : ''}
              />
              <p className="text-muted-foreground text-xs">
                {settings?.hasPassword
                  ? 'A password is saved. It is never shown again: leave the field empty to keep it, or enter a new one to replace it.'
                  : 'It is encrypted before being saved and is never shown again.'}
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Sender</CardTitle>
            <CardDescription>
              What recipients will see. Most relays reject a sender address they do not
              recognize.
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="fromAddress">Address</Label>
              <Input
                id="fromAddress"
                type="email"
                value={fromAddress}
                onChange={e => setFromAddress(e.target.value)}
                placeholder="no-reply@cleanarchitecture.com"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="fromName">Display name</Label>
              <Input id="fromName" value={fromName} onChange={e => setFromName(e.target.value)} />
            </div>
          </CardContent>
        </Card>

        {testResult && (
          <div
            className={
              testResult.ok
                ? 'rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900'
                : 'rounded-md border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-900'
            }
            role="status"
          >
            <p className="flex items-center gap-2 font-medium">
              {testResult.ok ? (
                <CheckCircle2Icon className="size-4" aria-hidden />
              ) : (
                <XCircleIcon className="size-4" aria-hidden />
              )}
              {testResult.ok ? 'Message sent' : 'Sending failed'}
            </p>
            {/* The relay's own words. "It failed" teaches nothing to someone configuring SMTP. */}
            <p className="mt-1 break-words">{testResult.message}</p>
          </div>
        )}

        <div className="flex flex-wrap items-center gap-2">
          <Button onClick={save} disabled={saveSettings.isPending}>
            {saveSettings.isPending ? 'Saving…' : 'Save'}
          </Button>

          <Button variant="outline" onClick={test} disabled={sendTest.isPending}>
            <SendIcon className="size-4" aria-hidden />
            {sendTest.isPending ? 'Sending…' : 'Send me a test message'}
          </Button>

          {source === EmailSettingsSource.Database && (
            <Button
              variant="ghost"
              className="text-muted-foreground"
              onClick={() => setConfirmingReset(true)}
            >
              Revert to environment settings
            </Button>
          )}
        </div>
      </div>

      <ConfirmDialog
        open={confirmingReset}
        onOpenChange={setConfirmingReset}
        title="Remove saved settings?"
        description="The deployment configuration will apply again. If there is none, no email will be sent."
        confirmLabel="Remove"
        destructive
        onConfirm={reset}
      />
    </>
  );
}
