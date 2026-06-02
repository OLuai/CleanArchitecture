import { useState } from 'react';
import { createFileRoute, Link, useNavigate } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';
import { toast } from 'sonner';
import { z } from 'zod';

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
import { useRegister } from '@/lib/auth';
import { ApiError } from '@/api/mutator/custom-fetch';

export const Route = createFileRoute('/register')({
  component: RegisterPage,
});

const fields = {
  userName: z.string().min(3, 'Username must be at least 3 characters.'),
  email: z.string().email('Please enter a valid email address.'),
  password: z.string().min(6, 'Password must be at least 6 characters.'),
};

function firstError(errors: unknown[]): string | null {
  const err = errors?.[0];
  if (!err) return null;
  if (typeof err === 'string') return err;
  if (typeof err === 'object' && err !== null && 'message' in err) {
    return String((err as { message: unknown }).message);
  }
  return String(err);
}

function RegisterPage() {
  const navigate = useNavigate();
  const register = useRegister();
  const [serverError, setServerError] = useState('');

  const form = useForm({
    defaultValues: { userName: '', email: '', password: '' },
    onSubmit: async ({ value }) => {
      setServerError('');
      try {
        await register.mutateAsync({ data: value });
        toast.success('Account created. You can now log in.');
        navigate({ to: '/login' });
      } catch (e) {
        if (e instanceof ApiError && e.problem?.errors) {
          const messages = Object.values(e.problem.errors).flat();
          setServerError(messages[0] ?? 'Registration failed.');
        } else {
          setServerError('Registration failed. Please try again.');
        }
      }
    },
  });

  return (
    <div className="mx-auto max-w-sm">
      <Card>
        <CardHeader>
          <CardTitle>Register</CardTitle>
          <CardDescription>Create an account with a username and email.</CardDescription>
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
            <form.Field name="userName" validators={{ onChange: fields.userName }}>
              {(field) => (
                <div className="space-y-2">
                  <Label htmlFor="userName">Username</Label>
                  <Input
                    id="userName"
                    autoComplete="username"
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => field.handleChange(e.target.value)}
                    aria-invalid={field.state.meta.errors.length > 0 || undefined}
                  />
                  {field.state.meta.isTouched && (
                    <p className="text-destructive text-sm">
                      {firstError(field.state.meta.errors)}
                    </p>
                  )}
                </div>
              )}
            </form.Field>

            <form.Field name="email" validators={{ onChange: fields.email }}>
              {(field) => (
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    autoComplete="email"
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => field.handleChange(e.target.value)}
                    aria-invalid={field.state.meta.errors.length > 0 || undefined}
                  />
                  {field.state.meta.isTouched && (
                    <p className="text-destructive text-sm">
                      {firstError(field.state.meta.errors)}
                    </p>
                  )}
                </div>
              )}
            </form.Field>

            <form.Field name="password" validators={{ onChange: fields.password }}>
              {(field) => (
                <div className="space-y-2">
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    type="password"
                    autoComplete="new-password"
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => field.handleChange(e.target.value)}
                    aria-invalid={field.state.meta.errors.length > 0 || undefined}
                  />
                  {field.state.meta.isTouched && (
                    <p className="text-destructive text-sm">
                      {firstError(field.state.meta.errors)}
                    </p>
                  )}
                </div>
              )}
            </form.Field>

            {serverError && (
              <p id="register-error" className="text-destructive text-sm" role="alert">
                {serverError}
              </p>
            )}

            <form.Subscribe selector={(s) => [s.canSubmit, s.isSubmitting]}>
              {([canSubmit, isSubmitting]) => (
                <Button type="submit" className="w-full" disabled={!canSubmit || isSubmitting}>
                  {isSubmitting ? 'Creating account…' : 'Register'}
                </Button>
              )}
            </form.Subscribe>

            <p className="text-muted-foreground text-center text-sm">
              Already have an account?{' '}
              <Link to="/login" className="text-primary underline-offset-4 hover:underline">
                Log in
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
