# CleanArchitecture

Clean Architecture solution on .NET 10 with .NET Aspire orchestration and a React/Vite SPA frontend.

## Commands

- **Build:** `dotnet build`
- **Run:** `dotnet run --project .\src\AppHost` (opens Aspire dashboard)
- **Test (all):** `dotnet test`
- **Test (single project):** `dotnet test .\tests\Domain.UnitTests`
- **Scaffold use case:** `dotnet new ca-usecase --name CreateFoo --feature-name Foos --usecase-type command --return-type int` (run from `src\Application`; install with `dotnet new install Clean.Architecture.Solution.Template::10.8.0` if missing)
- **Generate TypeScript API client:** `npm run generate-api` (from `src\Web\ClientApp`)
- **Frontend dev:** `npm start` (from `src\Web\ClientApp`; auto-runs `generate-api` via prestart hook)

## Architecture

```
src/AppHost          → Aspire orchestration entry point (run this to start)
src/Web              → ASP.NET Core API + serves SPA from ClientApp/
src/Application      → MediatR commands/queries, FluentValidation, AutoMapper
src/Domain           → Entities, value objects, domain events (no external deps)
src/Infrastructure   → EF Core (PostgreSQL/Npgsql), ASP.NET Identity
src/ServiceDefaults  → Aspire shared service configuration
src/Shared           → Constants (service names, DB name)
```

Dependency flow: Web → Application → Domain, Web → Infrastructure → Application → Domain. Shared is referenced by Web, Infrastructure, AppHost, and test projects.

## Frontend

- React 19 + Vite 8 + TypeScript (`.tsx`)
- **Tailwind CSS v4** (`@tailwindcss/vite`) + **shadcn/ui** components (`src/components/ui`, config in `components.json`, `cn` helper in `src/lib/utils.ts`)
- **TanStack Router** (file-based, routes in `src/routes`, generated `src/routeTree.gen.ts`), **TanStack Query**, **TanStack Form**, **TanStack Table**
- **Orval** generates a typed client + TanStack Query hooks from `src/Web/wwwroot/openapi/v1.json` into `src/Web/ClientApp/src/api/generated` (config: `orval.config.ts`). A custom fetch mutator (`src/api/mutator/custom-fetch.ts`) forces `credentials: 'include'` for cookie auth and throws an `ApiError` carrying the ProblemDetails body on non-2xx.
- Vite proxies `/api`, `/openapi`, `/scalar`, and weather forecast routes to the backend via Aspire service discovery env vars
- Regenerate the client with `npm run generate-api` (runs Orval); also runs automatically on `npm start`/`npm run build` via the `prestart`/`prebuild` hooks
- Auth: cookie-based ASP.NET Identity. Hooks in `src/lib/auth.ts` (`useSession`/`useLogin`/`useRegister`/`useLogout`); protected routes guard via `beforeLoad` + `ensureAuthenticated`. Login accepts username **or** email.

## Database

- PostgreSQL, provisioned by Aspire (containerized locally via `AddAzurePostgresFlexibleServer` → `RunAsContainer`)
- **No EF Core migrations.** Uses `EnsureDeletedAsync()` + `EnsureCreatedAsync()` on startup in development — the DB is recreated every run
- Connection string key: `ConnectionStrings:CleanArchitectureDb`
- The production AppHost also provisions Azure Container Apps (`AddAzureContainerAppEnvironment`)

## Testing

- **Framework:** NUnit (not xUnit), Shouldly for assertions, Moq for mocking
- **Functional tests** (`Application.FunctionalTests`): spin up a full Aspire `DistributedApplication` via `TestAppHost` with a real PostgreSQL container; use `Respawn` to reset DB between tests
- **Acceptance tests** (`Web.AcceptanceTests`): Playwright + Reqnroll (Gherkin `.feature` files); also spin up Aspire; headless when not debugging, slow-mo when attached
- **Integration tests** (`Infrastructure.IntegrationTests`): currently empty project
- Functional and acceptance tests require Docker (for PostgreSQL containers)

## Conventions

- Root namespace: `CleanArchitecture` (with underscore)
- C# file-scoped namespaces
- Private fields: `_camelCase`; private static fields: `s_camelCase`
- `TreatWarningsAsErrors` is enabled (`NU1608` suppressed)
- Central package versions in `Directory.Packages.props`
- Build output in `artifacts/` (not default `obj/`)
- Test service names defined in `src/Shared/Services.cs` constants