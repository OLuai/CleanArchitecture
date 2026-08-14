import { useState } from 'react';
import { createFileRoute, Link, useNavigate } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

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
import { useLogin } from '@/lib/auth';
import { apiErrorMessage } from '@/lib/api-error';
import { ApiError } from '@/api/mutator/custom-fetch';

export const Route = createFileRoute('/login')({
  validateSearch: (search: Record<string, unknown>): { returnUrl?: string } => ({
    returnUrl: typeof search.returnUrl === 'string' ? search.returnUrl : undefined,
  }),
  component: LoginPage,
});

function LoginPage() {
  const { returnUrl } = Route.useSearch();
  const navigate = useNavigate();
  const login = useLogin();
  const [error, setError] = useState('');

  const form = useForm({
    defaultValues: { login: '', password: '' },
    onSubmit: async ({ value }) => {
      setError('');
      try {
        await login.mutateAsync({ data: value });
        navigate({ to: returnUrl ?? '/' });
      } catch (e) {
        // 401 is the expected failure and its server-side detail is deliberately vague, so
        // phrase it here; anything else (429, 500, offline) carries a message worth showing.
        setError(
          e instanceof ApiError && e.status === 401
            ? 'Invalid username/email or password.'
            : apiErrorMessage(e, 'Login failed. Please try again.')
        );
      }
    },
  });

  return (
    <div className="mx-auto max-w-sm">
      <Card>
        <CardHeader>
          <CardTitle>Log in</CardTitle>
          <CardDescription>Use your username or email address.</CardDescription>
        </CardHeader>
        <CardContent>
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              e.stopPropagation();
              form.handleSubmit();
            }}
          >
            <form.Field name="login">
              {(field) => (
                <div className="space-y-2">
                  <Label htmlFor="login">Username or email</Label>
                  <Input
                    id="login"
                    autoComplete="username"
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => {
                      setError('');
                      field.handleChange(e.target.value);
                    }}
                    aria-invalid={error ? true : undefined}
                  />
                </div>
              )}
            </form.Field>

            <form.Field name="password">
              {(field) => (
                <div className="space-y-2">
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    type="password"
                    autoComplete="current-password"
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => {
                      setError('');
                      field.handleChange(e.target.value);
                    }}
                    aria-invalid={error ? true : undefined}
                  />
                </div>
              )}
            </form.Field>

            {error && (
              <p id="login-error" className="text-destructive text-sm" role="alert">
                {error}
              </p>
            )}

            <form.Subscribe selector={(s) => s.isSubmitting}>
              {(isSubmitting) => (
                <Button type="submit" className="w-full" disabled={isSubmitting}>
                  {isSubmitting ? 'Logging in…' : 'Log in'}
                </Button>
              )}
            </form.Subscribe>

            <p className="text-muted-foreground text-center text-sm">
              Don&apos;t have an account?{' '}
              <Link to="/register" className="text-primary underline-offset-4 hover:underline">
                Register
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
