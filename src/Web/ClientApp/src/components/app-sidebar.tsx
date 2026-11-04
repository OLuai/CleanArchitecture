import { Link } from '@tanstack/react-router';
import { Boxes } from 'lucide-react';

import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/components/ui/sidebar';
import { UserMenu } from '@/components/user-menu';
import { navigation, type NavItem } from '@/lib/navigation';
import { usePermissions } from '@/lib/permissions';

export function AppSidebar() {
  const { check } = usePermissions();

  const visible = (item: NavItem) => item.permission === undefined || check(item.permission);

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton asChild size="lg">
              <Link to="/">
                <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
                  <Boxes className="size-4" />
                </div>
                <span className="font-semibold">Clean Architecture</span>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent>
        {navigation.map((group, index) => {
          const items = group.items.filter(visible);
          // A group whose every entry is hidden by permissions must not leave its heading behind.
          if (items.length === 0) return null;

          return (
            <SidebarGroup key={group.title ?? index}>
              {group.title && <SidebarGroupLabel>{group.title}</SidebarGroupLabel>}
              <SidebarGroupContent>
                <SidebarMenu>
                  {items.map(item => (
                    <SidebarMenuItem key={item.to}>
                      <SidebarMenuButton asChild tooltip={item.title}>
                        <Link
                          to={item.to}
                          activeOptions={{ exact: item.exact }}
                          activeProps={{ 'data-active': true }}
                        >
                          <item.icon />
                          <span>{item.title}</span>
                        </Link>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          );
        })}
      </SidebarContent>

      <SidebarFooter>
        <UserMenu />
      </SidebarFooter>
    </Sidebar>
  );
}
