# Template Guide

This repository is a **personal `dotnet new` template** — an opinionated fork of
[jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)
modernized to a single, fixed stack. Use it as the base for every new project, and pull
improvements from the upstream base template over time.

- `origin`   → your fork (`https://github.com/OLuai/CleanArchitecture`)
- `upstream` → the base template (`https://github.com/jasontaylordev/CleanArchitecture`)

## What this template gives you

A ready-to-run Clean Architecture solution with **one** opinionated stack, in **two modes**:

| Mode | `--client-framework` | What you get |
|------|----------------------|--------------|
| React dashboard (default) | `React` | Everything below, including the SPA and the Playwright acceptance tests |
| Web API only | `None` | The same backend without `src/Web/ClientApp` or `tests/Web.AcceptanceTests` |

| Area | Choice |
|------|--------|
| Frontend | React 19 · Vite · TanStack Router/Query/Form/Table · shadcn/ui (Radix) · Tailwind v4 · Orval |
| Dashboard | Sidebar shell, breadcrumb, user menu, stats home, **user & role administration**, account page |
| API client | [Orval](https://orval.dev/) generates typed TanStack Query hooks from the OpenAPI document |
| API docs | Native `Microsoft.AspNetCore.OpenApi` + [Scalar](https://scalar.com/) (NSwag/Swagger removed) |
| Backend | ASP.NET Core (.NET 10), MediatR, FluentValidation, static `IEndpointGroup` endpoints, RFC 9110 ProblemDetails, permissions/RBAC |
| Auth | ASP.NET Identity — cookie for browsers, bearer tokens for everything else, selected per request |
| Data | PostgreSQL + EF Core, dedicated `Migration` worker project (migrate + seed at startup) |
| Orchestration | .NET Aspire (`src/AppHost`) — Postgres, pgAdmin, Migration, Web API, React dev server |
| Tests | `tests/TestAppHost` spins up real PostgreSQL for functional/acceptance tests (Respawn) |
| Deployment | `deploy/` — Docker Compose behind Traefik, plus a reusable GitHub Actions workflow |
| Extra projects | `src/Shared` (Aspire service-name constants), `src/Migration` |

---

## A. Using the template to create a new project

### Prerequisites

- .NET SDK **10.0.302+** (see `global.json`)
- Node.js **20+** and npm (React mode only)
- Docker / a container runtime (Aspire launches the Postgres + pgAdmin containers)

### 1. Install the template locally

From the root of this repository:

```powershell
dotnet new install .
```

This registers the `ca-sln` (solution) and `ca-usecase` (command/query scaffolder) templates.
Re-run it with `--force` after pulling changes to refresh the locally installed copy. To
uninstall: `dotnet new uninstall <path-to-this-repo>`.

### 2. Create a project

```powershell
dotnet new ca-sln -o MyProject                       # React dashboard
dotnet new ca-sln -o MyApi --client-framework None   # Web API only
```

`dotnet new` replaces the `CleanArchitecture` source name with `MyProject` everywhere —
namespaces (`MyProject.Web`, `MyProject.Application`, …), assembly names, the `.slnx`, the
database name (`CleanArchitectureDb` → `MyProjectDb`), etc.

> **Note:** `dotnet new` rewrites **every case variant** of the source name, lowercase and
> kebab-case included. Anything that must stay identical across projects therefore cannot be
> named after `CleanArchitecture` — see *Shared local infrastructure* below.

### 3. Run it

Follow the generated project's own `README.md` (first-time Postgres password setup, then):

```powershell
dotnet run --project ./src/AppHost
```

The Aspire dashboard opens with Postgres, pgAdmin, the `migration` worker, the Web API
(Scalar at `/scalar`) and — in React mode — the dev server.

Sign in with the seeded administrator: `administrator@localhost` / `Administrator1!`.

### Shared local infrastructure

**Every solution generated from this template shares one PostgreSQL container and one pgAdmin**,
each solution owning its own database inside them. That is why the container, volume and network
names carry no project name — `dotnet new` would rename them, every project would start its own
container, and they would all collide on the fixed host ports.

| What | Fixed name | Host port |
|------|-----------|-----------|
| PostgreSQL container | `ca-shared-postgres` | `5431` |
| Data volume | `ca-shared-pg-data` | — |
| pgAdmin container | `ca-shared-pgadmin` | `5050` |
| Traefik network (deployment) | `ca-shared-web` | — |

They are declared once in `src/Shared/Services.cs` (`Services.Shared`) and used from
`src/AppHost/Program.cs`. The database name (`Services.Database`) *is* renamed per project, which
is what keeps the solutions isolated from one another inside the shared server.

Because they share the container, they must all present the same password. Set it once per
machine — not in user secrets, which `dotnet new` gives each project its own copy of:

```powershell
setx CA_SHARED_PG_PWD "<the password the container was created with>"
```

Reopen the shell afterwards. The AppHost and the `scripts/db` EF tooling both read it. Without
the variable the AppHost falls back to a prompted Aspire parameter, which works for a single
solution but means the first one to run decides the container's password for all the others.

> The functional-test fixtures are deliberately **not** shared: `tests/TestAppHost` starts an
> unnamed, non-persistent PostgreSQL container so a test run can never touch development data.
> The **acceptance** tests are different — they start the real AppHost, so they use the shared
> container and require `CA_SHARED_PG_PWD`. They fail immediately with that message when it is
> unset, rather than waiting for a resource that can never become healthy.

### 4. Day-to-day: regenerate the API client

After changing the backend API surface, regenerate the typed frontend client:

```powershell
cd src/Web/ClientApp
npm run generate-api   # Orval reads src/Web/wwwroot/openapi/v1.json (emitted on build)
```

(`npm start` / `npm run build` run this automatically via the `prestart` / `prebuild` hooks.)

### 5. Where to add a feature

1. **Domain** — add an entity under `src/Domain/Entities` (derive from `BaseEntity` /
   `BaseAuditableEntity`).
2. **Application** — scaffold a command/query: `dotnet new ca-usecase -n CreateThing -fn Things -ut command -rt int`.
   Omit `-rt` for a command with no return value.
3. **Permission** — add a value to `src/Domain/Constants/Permissions.cs`, then grant it to the
   relevant roles in `src/Migration/Seed/RolePermissionMap.cs`. Administrator always receives the
   whole catalogue. The seeder both grants and **revokes**, so removing a line takes the
   permission away on the next deployment.
4. **Endpoint** — add a class implementing `IEndpointGroup` under `src/Web/Endpoints`, with
   `.RequireAuthorization($"Permission:{...}")` per route.
5. **Frontend** — `npm run generate-api`, add a TanStack route under `src/Web/ClientApp/src/routes`
   consuming the generated hook, gate it with `ensurePermission` / the `Can` component from
   `src/lib/permissions.tsx`, and add it to `src/lib/navigation.tsx` so it appears in the sidebar.

### 6. Listing and searching

`PaginatedList<T>`, `PaginatedQuery` and `SortMap<T>` in `src/Application/Common/Models` are the
canonical list-query building blocks. `SortMap` is an allow-list of sortable columns, so no
caller-supplied string ever reaches the query. `GetUsersQuery` is the worked example.

For accent- and case-insensitive search, use `SearchNormalizer.PreparePattern` with
`DbSearchExtensions.Unaccent` (mapped to the `f_unaccent` SQL function created by migration).
Add a trigram GIN index per searchable column:

```sql
CREATE INDEX IF NOT EXISTS ix_things_name_trgm
    ON "Things" USING gin (lower(f_unaccent("Name")) gin_trgm_ops);
```

### 7. Error handling

The backend turns every exception into RFC 9110 ProblemDetails
(`src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs`); throw `NotFoundException`,
`ForbiddenAccessException`, `ValidationException`, or `FriendlyException` for a custom status
and message. Anything unhandled becomes a JSON 500 rather than an empty body.

On the client, the fetch mutator throws `ApiError` (carrying the ProblemDetails) or
`NetworkError` (no response at all). In a form, pass the error to `applyServerErrors(form, e)`:
per-field validation errors land under their input, everything else comes back as a list for
`<FormErrorSummary>`. Outside a form, `apiErrorMessage(e, fallback)` gives a message to toast.

### 8. Configuration and environment variables

Configuration that describes the topology belongs in `src/AppHost/Program.cs`, not in
`appsettings.<Environment>.json`: Aspire owns the resources, so it should own the values that
point at them. Use `WithEnvironment("Section__Key", value)` and pass another resource's endpoint
rather than a hard-coded URL — the AppHost carries a worked example. Secrets go through
`builder.AddParameter(name, secret: true)`, which prompts once and stores in user secrets.

For deployed environments the same keys live in `deploy/.env`; see `deploy/README.md`.

---

## B. Updating this template from the base (`upstream`)

The base template keeps evolving — bug fixes, package bumps, new EF/Aspire patterns. Pull the
ones worth having.

### 1. Fetch the base

```powershell
git fetch upstream
git checkout -b sync-upstream            # work on a branch, never on main
git merge upstream/main
```

### 2. Resolving conflicts

This fork has **diverged heavily** from the base in a few areas. Expect conflicts mostly there,
and apply this policy:

| Area | Policy on conflict |
|------|--------------------|
| `src/Web/ClientApp/**` | **Keep ours** — the base ships Angular / an older React app. |
| `src/Web/**` backend (Program.cs, DI, Endpoints, Infrastructure) | **Keep ours.** Both sides converged on native OpenAPI + `IEndpointGroup` + ProblemDetails, but ours is a superset: RBAC, richer transformers, custom Users endpoints, rate limiting, forwarded headers. |
| `.template.config/template.json` | **Keep ours** — the base is multi-option (Angular, three databases); ours has one stack and two client modes. |
| `src/AppHost/**`, `src/Shared/**`, `src/Migration/**`, `tests/TestAppHost/**` | **Keep ours** — these don't exist, or differ structurally, in the base. |
| `Directory.Packages.props` | **Merge carefully** — take base version bumps, keep our Scalar/Aspire/Orval-side packages, and drop any re-introduced NSwag/SQLite/SQL Server packages. Keep `Aspire.Hosting.PostgreSQL` rather than the Azure variant. |
| `templates/ca-use-case/**` | **Take theirs** — we don't customise the scaffolder. |
| `src/Domain/**`, `src/Application/**` (core logic, behaviours, mappings) | **Review and usually take base** — shared Clean Architecture logic; adopt base fixes, then re-apply our `Permissions`/RBAC additions. |
| `tests/**` (non-DB) | **Review** — adopt base test improvements; keep our PostgreSQL/TestAppHost fixtures. |
| `.github/workflows/**`, `build/**` | **Review** — take base modernisation, then re-apply the `#if (!UseApiOnly)` gates on the npm and Playwright steps. |

Files the base may add that we deliberately don't use (NSwag configs, Angular `ClientApp`,
`ClientApp-React`, `Pages/`, `Templates/`, multi-DB `appsettings.*.json` / test-DB classes,
azd artifacts under `infra/` and `.azdo/`): **delete them after the merge** rather than wiring
them in.

### 3. After merging

```powershell
dotnet build CleanArchitecture.slnx
cd src/Web/ClientApp; npm install; npm run build
dotnet new install . --force
```

If the OpenAPI surface changed, run `npm run generate-api`, then smoke-test an instantiation
(Section C).

---

## C. Maintaining the template itself

### Smoke-test both modes

```powershell
./build/test.ps1
```

Generates, builds and tests the solution in each mode, and asserts that an API-only output
carries no `ClientApp`, no acceptance tests and no leftover conditional markers.
`.github/workflows/test-templates.yml` runs the same matrix in CI.

### Genericization tokens (when porting changes from a concrete project)

When copying code from a working instance (e.g. a project named `Foo_bar`), rewrite its name
back to the renameable `CleanArchitecture` token family so `dotnet new` can re-rename it:

| Concrete token | Template token |
|----------------|----------------|
| `Foo_bar` (PascalCase namespaces, assembly names) | `CleanArchitecture` |
| `FooBarDb` (database name) | `CleanArchitectureDb` |
| `foobar-*` (per-project container/volume names) | `cleanarchitecture-*` |

Shared-infrastructure names (`ca-shared-*`, `CA_SHARED_PG_PWD`) must be left alone — renaming
them per project is exactly the bug they exist to prevent.

### How the two modes are enforced

`.template.config/template.json` exposes a single parameter, `ClientFramework`
(`React` | `None`), from which `UseReact` and `UseApiOnly` are computed. Two mechanisms then
apply it:

1. **Source modifiers** physically exclude `src/Web/ClientApp/**`,
   `tests/Web.AcceptanceTests/**` and `deploy/Dockerfile.web`, and rename
   `deploy/Dockerfile.web-apionly` into its place.
2. **`#if (!UseApiOnly)` directives** gate the places that merely *reference* the SPA and would
   otherwise break the build: `src/Web/Web.csproj` (`SpaRoot`, `PublishRunWebpack`),
   `src/AppHost/Program.cs` (`AddJavaScriptApp`), `src/Shared/Services.cs` (`WebFrontend`),
   `src/Web/Program.cs` (`UseFileServer`, `MapFallbackToFile`), `CleanArchitecture.slnx`,
   `build/build.ps1` and `.github/workflows/build.yml`.

The remaining `UsePostgreSQL` / `UseSqlServer` / `UseSqlite` / `UseGithubActions` symbols are
pinned as constants so any base-inherited machinery keeps resolving to the single-stack branch.

`README.md` and `TEMPLATE_GUIDE.md` are excluded from generated output; `README-template.md` is
renamed to the new project's `README.md`.
