import type { LucideIcon } from 'lucide-react';
import {
  Cloud,
  Hash,
  LayoutDashboard,
  ListTodo,
  ShieldCheck,
  Users,
} from 'lucide-react';

import { Permission, type PermissionCheck } from '@/lib/permissions';

export interface NavItem {
  title: string;
  to: string;
  icon: LucideIcon;
  /** Omitted for links every signed-in user may see. */
  permission?: PermissionCheck;
  exact?: boolean;
}

export interface NavGroup {
  title?: string;
  items: NavItem[];
}

/**
 * The sidebar's contents and, through {@link breadcrumbLabel}, the breadcrumb labels. Adding a
 * route here is what makes it appear in the navigation; the permission is applied by the sidebar
 * (to hide the link) and independently by the route's own guard (to block direct access).
 */
export const navigation: NavGroup[] = [
  {
    items: [
      { title: 'Dashboard', to: '/', icon: LayoutDashboard, exact: true },
      { title: 'Tasks', to: '/todo', icon: ListTodo, permission: Permission.TodoListsView },
      { title: 'Weather', to: '/weather', icon: Cloud, permission: Permission.WeatherForecastsView },
      { title: 'Counter', to: '/counter', icon: Hash },
    ],
  },
  {
    title: 'Administration',
    items: [
      { title: 'Users', to: '/admin/users', icon: Users, permission: Permission.UsersView },
      { title: 'Roles', to: '/admin/roles', icon: ShieldCheck, permission: Permission.RolesView },
    ],
  },
];

const extraLabels: Record<string, string> = {
  '/admin': 'Administration',
  '/account': 'My account',
  '/login': 'Log in',
  '/register': 'Register',
};

const titleByPath = new Map<string, string>([
  ...navigation.flatMap(group => group.items.map(item => [item.to, item.title] as const)),
  ...Object.entries(extraLabels),
]);

/** Human-readable label for a path segment prefix, falling back to a title-cased segment. */
export function breadcrumbLabel(path: string): string {
  const known = titleByPath.get(path);
  if (known) return known;

  const segment = path.slice(path.lastIndexOf('/') + 1);
  return segment.charAt(0).toUpperCase() + segment.slice(1);
}

/** Cumulative path prefixes for a location, excluding the root. `/admin/users` → `['/admin', '/admin/users']`. */
export function breadcrumbTrail(pathname: string): string[] {
  const segments = pathname.split('/').filter(Boolean);

  return segments.map((_, index) => '/' + segments.slice(0, index + 1).join('/'));
}
