import { createFileRoute, Link } from '@tanstack/react-router';
import { ListTodo, ShieldCheck, Users } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

import { PageHeader } from '@/components/page-header';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { useGetRoles } from '@/api/generated/roles/roles';
import { useGetUsers } from '@/api/generated/users/users';
import { useGetTodoLists } from '@/api/generated/todo-lists/todo-lists';
import { successData } from '@/lib/api';
import { ensureAuthenticated } from '@/lib/auth';
import { Can, Permission, usePermissions } from '@/lib/permissions';

export const Route = createFileRoute('/')({
  beforeLoad: async ({ context }) => {
    await ensureAuthenticated(context.queryClient).catch(() => {});
  },
  component: DashboardPage,
});

function StatCard({
  title,
  value,
  isLoading,
  icon: Icon,
  to,
}: {
  title: string;
  value: number | undefined;
  isLoading: boolean;
  icon: LucideIcon;
  to: string;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="text-muted-foreground size-4" />
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-8 w-16" />
        ) : (
          <div className="text-2xl font-semibold">{value ?? '—'}</div>
        )}
        <Link to={to} className="text-muted-foreground mt-1 inline-block text-xs hover:underline">
          View all
        </Link>
      </CardContent>
    </Card>
  );
}

function DashboardPage() {
  const { has, isAuthenticated } = usePermissions();

  // Each card is fetched only when the viewer may see the underlying resource, so the dashboard
  // never fires a request that would come back 403.
  const canViewUsers = has(Permission.UsersView);
  const canViewRoles = has(Permission.RolesView);
  const canViewTodos = has(Permission.TodoListsView);

  // PageSize 1 is enough: only the total count is displayed.
  const users = useGetUsers({ PageNumber: 1, PageSize: 1 }, { query: { enabled: canViewUsers } });
  const roles = useGetRoles({ query: { enabled: canViewRoles } });
  const todoLists = useGetTodoLists({ query: { enabled: canViewTodos } });

  if (!isAuthenticated) {
    return (
      <>
        <PageHeader
          title="Clean Architecture"
          description="Sign in to see your dashboard."
        />
        <Card>
          <CardContent className="pt-6">
            <Link to="/login" className="text-primary underline-offset-4 hover:underline">
              Log in
            </Link>{' '}
            to continue.
          </CardContent>
        </Card>
      </>
    );
  }

  return (
    <>
      <PageHeader title="Dashboard" description="An overview of your workspace." />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Can permission={Permission.UsersView}>
          <StatCard
            title="Users"
            value={successData(users.data)?.totalCount}
            isLoading={users.isLoading}
            icon={Users}
            to="/admin/users"
          />
        </Can>

        <Can permission={Permission.RolesView}>
          <StatCard
            title="Roles"
            value={successData(roles.data)?.length}
            isLoading={roles.isLoading}
            icon={ShieldCheck}
            to="/admin/roles"
          />
        </Can>

        <Can permission={Permission.TodoListsView}>
          <StatCard
            title="Task lists"
            value={successData(todoLists.data)?.lists?.length}
            isLoading={todoLists.isLoading}
            icon={ListTodo}
            to="/todo"
          />
        </Can>
      </div>
    </>
  );
}
