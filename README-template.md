# CleanArchitecture

This project was generated from a personal [Clean Architecture Solution Template](caRepositoryUrl) — a .NET Aspire + PostgreSQL backend built on Clean Architecture, with an optional React dashboard.

> Sections marked **(React mode)** apply only if the solution was generated with the React
> dashboard. In API-only mode there is no `src/Web/ClientApp` and no `tests/Web.AcceptanceTests`.

## Stack

- **Backend**: ASP.NET Core (.NET 10), Clean Architecture, MediatR, FluentValidation, EF Core (PostgreSQL), native OpenAPI + [Scalar](https://scalar.com/).
- **Orchestration**: [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) (`src/AppHost`) — Postgres + pgAdmin containers, a `Migration` worker, the Web API, and the React dev server.
- **Auth**: ASP.NET Identity with permission-based RBAC. Browsers use the application cookie; other clients obtain a bearer token from `POST /api/Users/identity/login?useCookies=false`. A policy scheme picks per request, so endpoints never care which was used.
- **Frontend (React mode)** (`src/Web/ClientApp`): React 19, [Vite](https://vite.dev/), [TanStack](https://tanstack.com/) Router/Query/Form/Table, [shadcn/ui](https://ui.shadcn.com/) (Radix + Tailwind v4), [Orval](https://orval.dev/) (typed TanStack Query hooks generated from the OpenAPI document). Ships as a dashboard: sidebar shell, user and role administration, account page.

## Build

Run `dotnet build` to build the solution.

## Run

```bash
dotnet run --project ./src/AppHost
```

The Aspire dashboard opens automatically, showing the application URLs and logs. The Web API (with Scalar at `/scalar`), Postgres, pgAdmin, the `migration` worker — and, in React mode, the dev server — all appear as resources.

Sign in with the seeded administrator: **`administrator@localhost`** / **`Administrator1!`**. Change this before deploying anywhere.

## Database (PostgreSQL, EF Core Migrations)

The solution uses **Aspire-orchestrated PostgreSQL** with **EF Core Migrations** for schema management and a dedicated **`Migration` worker project** that applies migrations and seeds data at startup.

### Aspire resources (declared in `src/AppHost`)

| Resource    | Description                                                              | Host port |
|-------------|--------------------------------------------------------------------------|-----------|
| `postgres`  | **Shared** Postgres container `ca-shared-postgres`, volume `ca-shared-pg-data` | `5431` |
| `pgadmin`   | **Shared** pgAdmin UI `ca-shared-pgadmin`, persistent                    | `5050`    |
| `migration` | Worker that applies migrations + seed, then exits                        | —         |
| `webapi`    | Web API (waits for `migration` completion before starting)               | dynamic   |

> **The PostgreSQL container and pgAdmin are shared with every other solution generated from this
> template.** This solution owns its own database (`CleanArchitectureDb`) inside that shared
> server; the container itself is reused rather than duplicated, which is why its name carries no
> project name and why the host ports are fixed.
>
> The consequence: every such solution must present the **same** password, so it comes from a
> machine-wide environment variable rather than from this project's user secrets.

### First-time setup

1. **Set the shared Postgres password** as a user environment variable. The AppHost and the EF
   Core scripts both read it:

   ```powershell
   setx CA_SHARED_PG_PWD "<choose-a-password>"
   ```

   Close and reopen the shell after `setx`. If the `ca-shared-postgres` container already exists
   from another solution, use the password it was created with — PostgreSQL keeps the credentials
   from its first initialisation.

2. **Restore the local `dotnet-ef` tool** (manifest in `.config/dotnet-tools.json`):

   ```powershell
   dotnet tool restore
   ```

3. **Start Aspire once** so the shared container and volume exist (or are picked up if another
   solution already created them):

   ```powershell
   dotnet run --project ./src/AppHost
   ```

4. **Create the initial migration** (the `DbContext` lives in `src/Infrastructure`):

   ```powershell
   ./scripts/db/Add-Migration.ps1 Initial
   ```

5. Restart Aspire. The `migration` worker applies pending migrations, runs the **DevelopmentSeeder** (roles + admin + sample TodoList) in dev or the **ProductionSeeder** (roles + admin only) elsewhere, then exits. `webapi` starts via `.WaitForCompletion(migration)`.

### EF Core scripts (`scripts/db/`)

The scripts wrap `dotnet ef` with the correct `--project src/Infrastructure --startup-project src/Migration` arguments and inject the connection string from `CA_SHARED_PG_PWD`:

```powershell
./scripts/db/Add-Migration.ps1 <Name>     # create a new migration
./scripts/db/Remove-Migration.ps1         # remove the last (unapplied) migration
./scripts/db/Update-Database.ps1 [<Name>] # apply migrations up to <Name> (or latest)
```

### Seed data — dev vs prod

| Seeder                                    | Roles | Default admin | Sample TodoList |
|-------------------------------------------|:-----:|:-------------:|:---------------:|
| `ProductionSeeder` (non-Development envs) |  ✅   |      ✅       |       ❌        |
| `DevelopmentSeeder` (Development env)      |  ✅   |      ✅       |       ✅        |

Both seeders are idempotent. The data volume `ca-shared-pg-data` is persistent across Aspire restarts and shared with the other solutions generated from this template — dropping it wipes their databases too.

## API client generation (Orval, React mode)

The Web API emits an OpenAPI document to `src/Web/wwwroot/openapi/v1.json` on build. The frontend regenerates its typed client + TanStack Query hooks from it:

```powershell
cd src/Web/ClientApp
npm run generate-api
```

`npm start` and `npm run build` run this automatically (via the `prestart` / `prebuild` hooks). Every exception is translated to an [RFC 9110 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc9110) body by `ProblemDetailsExceptionHandler`; the custom fetch mutator (`src/Web/ClientApp/src/api/mutator/custom-fetch.ts`) surfaces those typed errors as an `ApiError` to the UI.

## Authorization

Permissions live in `src/Domain/Constants/Permissions.cs` and are stored as **role claims**: a
user's effective permissions are the union of their roles'. To add one:

1. Add the constant to `Permissions.cs`.
2. Grant it to the relevant roles in `src/Migration/Seed/RolePermissionMap.cs`. The seeder both
   grants and **revokes**, so removing a line takes the permission away on the next deployment.
   Administrator always receives the whole catalogue.
3. Protect the endpoint with `.RequireAuthorization($"Permission:{Permissions.Area.Action}")`,
   and/or the MediatR request with `[Authorize(Permissions = Permissions.Area.Action)]`.

The catalogue is published into the OpenAPI document, so the frontend's `Permission` enum is
generated rather than maintained by hand — gate UI with `<Can permission={...}>` and routes with
`ensurePermission(...)`.

Roles and per-user role assignment are managed from **/admin/roles** and **/admin/users** in the
dashboard. Changing them refreshes the affected users' sessions within five minutes.

## Code Scaffolding

Scaffold new commands and queries from the `./src/Application/` folder:

```bash
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

## Test

The solution contains unit, integration, functional, and acceptance tests. Functional and acceptance tests spin up a real PostgreSQL container via `tests/TestAppHost` (Aspire) and reset state with Respawn.

```bash
dotnet test
```

## Deployment

`deploy/` holds a self-hosted deployment: a Docker Compose stack (PostgreSQL, the migration job,
the web app) published through a shared Traefik that terminates TLS. `deploy/.env.example`
documents every configuration value the stack needs, and `deploy/README.md` covers first-time
host setup. `.github/workflows/_deploy.yml` is a reusable workflow that builds and pushes both
images and runs the deployment.
