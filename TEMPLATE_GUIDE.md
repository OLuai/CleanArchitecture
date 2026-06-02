# Template Guide

This repository is a **personal `dotnet new` template** — an opinionated fork of
[jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)
modernized to a single, fixed stack. Use it as the base for every new project, and pull
improvements from the upstream base template over time.

- `origin`   → your fork (`https://github.com/OLuai/CleanArchitecture`)
- `upstream` → the base template (`https://github.com/jasontaylordev/CleanArchitecture`)

## What this template gives you

A ready-to-run Clean Architecture solution with **one** opinionated stack (no Angular / SQLite /
SQL Server / API-only options — those were removed):

| Area | Choice |
|------|--------|
| Frontend | React 19 · Vite · TanStack Router/Query/Form/Table · shadcn/ui (Radix) · Tailwind v4 · Orval |
| API client | [Orval](https://orval.dev/) generates typed TanStack Query hooks from the OpenAPI document |
| API docs | Native `Microsoft.AspNetCore.OpenApi` + [Scalar](https://scalar.com/) (NSwag/Swagger removed) |
| Backend | ASP.NET Core (.NET 10), MediatR, FluentValidation, static `IEndpointGroup` endpoints, RFC 9110 ProblemDetails, permissions/RBAC |
| Data | PostgreSQL + EF Core, dedicated `Migration` worker project (migrate + seed at startup) |
| Orchestration | .NET Aspire (`src/AppHost`) — Postgres, pgAdmin, Migration, Web API, React dev server |
| Tests | `tests/TestAppHost` spins up real PostgreSQL for functional/acceptance tests (Respawn) |
| Extra projects | `src/Shared` (Aspire service-name constants), `src/Migration` |

---

## A. Using the template to create a new project

### Prerequisites

- .NET SDK **10.0.201+** (see `global.json`)
- Node.js **20+** and npm (for the React app)
- Docker / a container runtime (Aspire launches the Postgres + pgAdmin containers)

### 1. Install the template locally

From the root of this repository:

```powershell
dotnet new install .
```

This registers the `ca-sln` (solution) and `ca-usecase` (command/query scaffolder) templates.
Re-run it after pulling changes to refresh the locally installed copy. To uninstall:
`dotnet new uninstall <path-to-this-repo>`.

### 2. Create a project

```powershell
dotnet new ca-sln -o MyProject
```

`dotnet new` replaces the `CleanArchitecture` source name with `MyProject` everywhere —
namespaces (`MyProject.Web`, `MyProject.Application`, …), assembly names, the `.slnx`, the
database name (`CleanArchitectureDb` → `MyProjectDb`), etc.

> **Note:** lowercase/UPPERCASE constants that are *not* PascalCase `CleanArchitecture`
> (the Aspire container names `cleanarchitecture-postgres` / `-pg-data` / `-pgadmin`, and the
> `CLEANARCHITECTURE_PG_PWD` env var used by the EF scripts) are intentionally **not** renamed —
> they are machine-local conventions and stay constant across projects. Rename them by hand only
> if you need per-project container isolation.

### 3. Run it

Follow the generated project's own `README.md` (first-time Postgres password setup, then):

```powershell
dotnet run --project ./src/AppHost
```

The Aspire dashboard opens with Postgres, pgAdmin, the `migration` worker, the Web API
(Scalar at `/scalar`) and the React dev server.

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
3. **Permission** — add a value to `src/Domain/Constants/Permissions.cs`
   (exposed to the frontend via `PermissionsDocumentTransformer`).
4. **Endpoint** — add a class implementing `IEndpointGroup` under `src/Web/Endpoints`.
5. **Frontend** — `npm run generate-api`, then add a TanStack route under
   `src/Web/ClientApp/src/routes` consuming the generated hook; gate it with the `Can`
   component / `ensurePermission` guard from `src/lib/permissions.tsx`.

---

## B. Updating this template from the base (`upstream`)

The base template (`jasontaylordev/CleanArchitecture`) keeps evolving — bug fixes, package
bumps, new EF/Aspire patterns. Pull the ones worth having.

### 1. Fetch the base

```powershell
git fetch upstream
git checkout -b sync-upstream            # work on a branch, never on main
git merge upstream/main                  # or: git merge upstream/<tag>
```

Prefer **cherry-picking** specific fixes when you only want a targeted change:

```powershell
git log --oneline upstream/main          # find the commit
git cherry-pick <sha>
```

### 2. Resolving conflicts

This fork has **diverged heavily** from the base in a few areas. Expect conflicts mostly there,
and apply this policy:

| Area | Policy on conflict |
|------|--------------------|
| `src/Web/ClientApp/**` | **Keep ours** — base ships Angular / old React; we use the modern React stack. |
| `src/Web/**` backend (Program.cs, DI, Endpoints, Infrastructure) | **Keep ours** — base uses NSwag/Swagger + `EndpointGroupBase`; we use Scalar + native OpenAPI + `IEndpointGroup` + ProblemDetails transformers. |
| `.template.config/template.json` | **Keep ours** — base is multi-option; ours is the single fixed stack. |
| `src/AppHost/**`, `src/Shared/**`, `src/Migration/**`, `tests/TestAppHost/**` | **Keep ours** — these don't exist (or differ structurally) in the base. |
| `Directory.Packages.props` | **Merge carefully** — take base version bumps, but keep our Scalar/Aspire/Reqnroll/Orval-side packages and drop any re-introduced NSwag/SQLite/SQL Server packages. |
| `src/Domain/**`, `src/Application/**` (core logic, behaviours, mappings) | **Review and usually take base** — this is shared Clean Architecture logic; adopt base bug fixes, but re-apply our `Permissions`/RBAC additions if they conflict. |
| `tests/**` (non-DB) | **Review** — adopt base test improvements; keep our PostgreSQL/TestAppHost-based fixtures. |

Files the base may add that we deliberately don't use (NSwag configs, Angular `ClientApp`,
`ClientApp-React`, `Pages/`, `Templates/`, multi-DB `appsettings.*.json` / test-DB classes):
**delete them after the merge** rather than wiring them in.

### 3. After merging

```powershell
dotnet build CleanArchitecture.slnx          # backend still builds
cd src/Web/ClientApp && npm install && npm run build   # frontend still builds
dotnet new install .                          # refresh the locally installed template
```

If the OpenAPI surface changed, run `npm run generate-api` and commit the result of a clean
instantiation test (Section C).

---

## C. Maintaining the template itself

### Smoke-test an instantiation

```powershell
dotnet new install .
dotnet new ca-sln -o $env:TEMP\TplTest
# Verify no stale tokens leaked and namespaces are correct:
Select-String -Path $env:TEMP\TplTest\* -Pattern 'prono' -Recurse   # expect: no matches
```

### Genericization tokens (when porting more changes from a concrete project)

When copying code from a working instance (e.g. a project named `Foo_bar`), rewrite its name
back to the renameable `CleanArchitecture` token family so `dotnet new` can re-rename it:

| Concrete token | Template token |
|----------------|----------------|
| `Foo_bar` (PascalCase namespaces, assembly names) | `CleanArchitecture` |
| `FooBarDb` (database name) | `CleanArchitectureDb` |
| `foobar-*` (container/volume names) | `cleanarchitecture-*` |
| `FOOBAR_PG_PWD` (env var) | `CLEANARCHITECTURE_PG_PWD` |

### How the single stack is enforced

`.template.config/template.json` no longer exposes `ClientFramework` / `Database` / `UseAspire`
parameters. Instead it pins the formerly-computed symbols as **constants**
(`UseAspire=true`, `UsePostgreSQL=true`, `UseApiOnly=false`, …) so the `#if (...)` directives
that still live in base-inherited machinery files (`build.cake`, `.github/workflows/build.yml`,
`infra/main.bicep`) keep resolving to the correct single-stack branch during instantiation.
`README.md` and `TEMPLATE_GUIDE.md` are excluded from generated output; `README-template.md`
is renamed to the new project's `README.md`.
