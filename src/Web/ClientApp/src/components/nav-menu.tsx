import { Link, useNavigate } from '@tanstack/react-router';

import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/theme-toggle';
import { useSession, useLogout } from '@/lib/auth';
import { Can, Permission } from '@/lib/permissions';

const activeProps = { className: 'bg-accent text-accent-foreground' };

export function NavMenu() {
  const { isAuthenticated } = useSession();
  const logout = useLogout();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout.mutateAsync({ data: {} });
    navigate({ to: '/login' });
  };

  return (
    <header className="border-b">
      <nav className="container mx-auto flex h-14 items-center gap-2 px-4">
        <Link to="/" className="mr-2 font-semibold">
          Clean Architecture
        </Link>
        <div className="flex items-center gap-1">
          <Button asChild variant="ghost" size="sm">
            <Link to="/" activeOptions={{ exact: true }} activeProps={activeProps}>
              Home
            </Link>
          </Button>
          <Button asChild variant="ghost" size="sm">
            <Link to="/counter" activeProps={activeProps}>
              Counter
            </Link>
          </Button>
          <Can permission={Permission.WeatherForecastsView}>
            <Button asChild variant="ghost" size="sm">
              <Link to="/weather" activeProps={activeProps}>
                Weather
              </Link>
            </Button>
          </Can>
          <Can permission={Permission.TodoListsView}>
            <Button asChild variant="ghost" size="sm">
              <Link to="/todo" activeProps={activeProps}>
                Tasks
              </Link>
            </Button>
          </Can>
        </div>
        <div className="ml-auto flex items-center gap-2">
          {isAuthenticated ? (
            <Button variant="ghost" size="sm" onClick={handleLogout}>
              Log out
            </Button>
          ) : (
            <>
              <Button asChild variant="ghost" size="sm">
                <Link to="/login">Log in</Link>
              </Button>
              <Button asChild variant="ghost" size="sm">
                <Link to="/register">Register</Link>
              </Button>
            </>
          )}
          <ThemeToggle />
        </div>
      </nav>
    </header>
  );
}
