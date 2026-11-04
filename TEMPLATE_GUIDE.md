# Template Guide

This repository is a **personal `dotnet new` template** — an opinionated fork of
[jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)
modernized to one opinionated backend stack, with an optional React dashboard. Use it as the
base for every new project, and pull improvements from the upstream base template over time.

- `origin`   → your fork (`https://github.com/OLuai/CleanArchitecture`)
- `upstream` → the base template (`https://github.com/jasontaylordev/CleanArchitecture`)

## What this template gives you

A ready-to-run Clean Architecture solution with **one** opinionated backend stack (no Angular /
SQLite / SQL Server options — those were removed). The only option is the client:
`--client-framework React` (default) or `--client-framework None` for a Web API only.

| Area | Choice |
|------|--------|
| Frontend (React mode) | React 19 · Vite · TanStack Router/Query/Form/Table · shadcn/ui (Radix) · Tailwind v4 · Orval |
| API client | [Orval](https://orval.dev/) generates typed TanStack Query hooks from the OpenAPI document |
| API docs | Native `Microsoft.AspNetCore.OpenApi` + [Scalar](https://scalar.com/) (NSwag/Swagger removed) |
| Backend | ASP.NET Core (.NET 10), MediatR, FluentValidation, static `IEndpointGroup` endpoints, RFC 9110 ProblemDetails, permissions/RBAC |
| Data | PostgreSQL + EF Core, dedicated `Migration` worker project (migrate + seed at startup), UUID v7 keys and HiLo business codes |
| Email | SMTP through MailKit, configurable from `/admin/email`; Mailpit in development |
| Orchestration | .NET Aspire (`src/AppHost`) — Postgres, pgAdmin, Mailpit, Migration, Web API, React dev server (React mode) |
| Tests | `tests/TestAppHost` spins up real PostgreSQL for functional/acceptance tests (Respawn) |
| Extra projects | `src/Shared` (Aspire service-name constants), `src/Migration` |

---

## A. Using the template to create a new project

### Prerequisites

- .NET SDK **10.0.403+** (see `global.json`)
- Node.js **24+** and npm (React mode only)
- Docker / a container runtime (Aspire launches the Postgres + pgAdmin containers)

### 1. Install the template locally

From the root of this repository, on the branch you want to use:

```powershell
.\build\repack.ps1                  # version 1.0.<commit count>, "-dirty" with local changes
.\build\repack.ps1 -Version 1.2.0   # or an explicit version
```

It packs the template as `OLuai.CleanArchitecture.Template` (`artifacts/packages/*.nupkg`) and
installs that package in place of any previous install. This registers the `ca-sln` (solution)
and `ca-usecase` (command/query scaffolder) templates. Because the package is a snapshot,
switching branches in this folder no longer changes what `dotnet new ca-sln` generates: re-run
the script when you want the new state. To uninstall:
`dotnet new uninstall OLuai.CleanArchitecture.Template`.

`dotnet new install .` still works, but installs the folder itself: the template then follows
whatever branch is checked out. Keep it for iterating on the template.

**On another machine**, install a released version from GitHub Packages (publishing a GitHub
release tagged `vX.Y.Z` runs `.github/workflows/release.yml`):

```powershell
dotnet nuget add source https://nuget.pkg.github.com/OLuai/index.json -n github-oluai -u OLuai -p <PAT with read:packages>
dotnet new install OLuai.CleanArchitecture.Template
```

### 2. Create a project

```powershell
dotnet new ca-sln -o MyProject                              # API + React dashboard
dotnet new ca-sln -o MyProject --client-framework None      # Web API only
```

In API-only mode there is no `src/Web/ClientApp`, no `tests/Web.AcceptanceTests`, no Node or
Playwright step in CI, and `/` redirects to the Scalar API reference.

`dotnet new` replaces the `CleanArchitecture` source name with `MyProject` everywhere —
namespaces (`MyProject.Web`, `MyProject.Application`, …), assembly names, the `.slnx`, the
database name (`CleanArchitectureDb` → `MyProjectDb`), etc.

> **Note:** the shared development infrastructure is deliberately **not** named after the
> project: the `ca-shared-postgres` / `ca-shared-pg-data` / `ca-shared-pgadmin` containers and
> volume (`Services.Shared`) and the machine-wide `CA_SHARED_PG_PWD` password stay identical in
> every generated solution, so they all use one PostgreSQL instance, each with its own database.

### 3. Run it

Follow the generated project's own `README.md` (first-time Postgres password setup, then):

```powershell
dotnet run --project ./src/AppHost
```

The Aspire dashboard opens with Postgres, pgAdmin, Mailpit, the `migration` worker, the Web API
(Scalar at `/scalar`) and, in React mode, the React dev server.

### 4. Day-to-day: regenerate the API client (React mode)

After changing the backend API surface, regenerate the typed frontend client:

```powershell
cd src/Web/ClientApp
npm run generate-api   # Orval reads src/Web/wwwroot/openapi/v1.json (emitted on build)
```

(`npm start` / `npm run build` run this automatically via the `prestart` / `prebuild` hooks.)

### 5. Where to add a feature

1. **Domain** — add an entity under `src/Domain/Entities` (derive from `BaseEntity<Guid>` /
   `BaseAuditableEntity<Guid>`; implement `IHasCode` and call `RegisterCode<T>("XYZ")` when people
   need a readable reference).
2. **Application** — scaffold a command/query: `dotnet new ca-usecase -n CreateThing -fn Things -ut command -rt int`.
3. **Permission** — add a value to `src/Domain/Constants/Permissions.cs`
   (exposed to the frontend via `PermissionsDocumentTransformer`).
4. **Endpoint** — add a class implementing `IEndpointGroup` under `src/Web/Endpoints`.
5. **Frontend** (React mode) — `npm run generate-api`, then add a TanStack route under
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
| `.template.config/template.json` | **Keep ours** — base offers Angular/SQLite/SQL Server; ours only offers React or API-only. |
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
.\build\test.ps1                              # both client modes still generate and pass
.\build\repack.ps1                            # refresh the installed template package
```

If the OpenAPI surface changed, run `npm run generate-api` and commit the result of a clean
instantiation test (Section C).

---

## C. Maintaining the template itself

### Smoke-test an instantiation

```powershell
.uild	est.ps1                                   # both modes: React and None
.uild	est.ps1 -ClientFramework None             # one mode only
```

It installs this folder into a private template registry (`--debug:custom-hive`, so the package
installed by `repack.ps1` is left alone), generates a solution per client mode, checks that no
`UseApiOnly` marker leaked and that API-only output has no `ClientApp`, then builds and runs the
tests. `.github/workflows/test-templates.yml` runs the same matrix on every push to `main`.

### Genericization tokens (when porting more changes from a concrete project)

When copying code from a working instance (e.g. a project named `Foo_bar`), rewrite its name
back to the renameable `CleanArchitecture` token family so `dotnet new` can re-rename it:

| Concrete token | Template token |
|----------------|----------------|
| `Foo_bar` (PascalCase namespaces, assembly names) | `CleanArchitecture` |
| `foo_bar` / `FOO_BAR` (lower/upper case) | `cleanarchitecture` / `CLEANARCHITECTURE` |
| `FooBarDb` (database name) | `CleanArchitectureDb` |

Never genericize the shared development infrastructure (`ca-shared-*`, `CA_SHARED_PG_PWD`): it
must stay identical across projects.

### How the client option works

`.template.config/template.json` exposes one parameter, `ClientFramework` (`React` or `None`),
from which `UseReact` and `UseApiOnly` are computed. The other formerly-computed symbols are pinned
as **constants** (`UseAspire=true`, `UsePostgreSQL=true`, `UseAngular=false`, …).

- **Whole files:** in API-only mode the `(UseApiOnly)` modifier excludes `src/Web/ClientApp/**`
  and `tests/Web.AcceptanceTests/**`, and renames `.claude/launch.api-only.json` to
  `.claude/launch.json` (JSON cannot carry conditional comments).
- **Inside files:** `#if (!UseApiOnly)` / `#if (UseApiOnly)` blocks in `src/Web/Program.cs`,
  `src/Web/Web.csproj`, `src/AppHost/Program.cs`, `CleanArchitecture.slnx`,
  `Directory.Packages.props` and `.github/workflows/build.yml`. In this repository the C#
  compiler sees `UseApiOnly` as undefined, so the React branch is the one that builds here.
- **Documentation:** `README-template.md` and `AGENTS.md` mark React-only sections as such rather
  than using conditionals.

`README.md` and `TEMPLATE_GUIDE.md` are excluded from generated output; `README-template.md`
is renamed to the new project's `README.md`.
