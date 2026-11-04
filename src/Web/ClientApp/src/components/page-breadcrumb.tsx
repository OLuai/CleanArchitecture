import { Fragment } from 'react';
import { Link, useRouterState } from '@tanstack/react-router';

import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb';
import { breadcrumbLabel, breadcrumbTrail } from '@/lib/navigation';

/**
 * Breadcrumb derived from the current path, with labels resolved through the navigation map.
 * Intermediate segments that are not routes of their own (for example `/admin`) render as plain
 * text rather than dead links.
 */
export function PageBreadcrumb() {
  const pathname = useRouterState({ select: state => state.location.pathname });
  const trail = breadcrumbTrail(pathname);

  return (
    <Breadcrumb>
      <BreadcrumbList>
        <BreadcrumbItem>
          {trail.length === 0 ? (
            <BreadcrumbPage>Dashboard</BreadcrumbPage>
          ) : (
            <BreadcrumbLink asChild>
              <Link to="/">Dashboard</Link>
            </BreadcrumbLink>
          )}
        </BreadcrumbItem>

        {trail.map((path, index) => {
          const isLast = index === trail.length - 1;
          const label = breadcrumbLabel(path);

          return (
            <Fragment key={path}>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                {isLast ? (
                  <BreadcrumbPage>{label}</BreadcrumbPage>
                ) : (
                  <span className="text-muted-foreground">{label}</span>
                )}
              </BreadcrumbItem>
            </Fragment>
          );
        })}
      </BreadcrumbList>
    </Breadcrumb>
  );
}
