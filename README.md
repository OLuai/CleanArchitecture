# Clean Architecture Solution Template — React + Aspire (personal fork)

An opinionated **personal `dotnet new` template**, forked from
[jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) and
modernized to a single fixed stack. It is the base for all my new projects.

## Stack

| Area | Choice |
|------|--------|
| Frontend | React 19 · Vite · TanStack Router/Query/Form/Table · shadcn/ui (Radix) · Tailwind v4 · [Orval](https://orval.dev/) |
| Dashboard | Sidebar shell · stats home · **user & role administration** · account page |
| API docs | Native `Microsoft.AspNetCore.OpenApi` + [Scalar](https://scalar.com/) |
| Backend | ASP.NET Core (.NET 10) · MediatR · FluentValidation · `IEndpointGroup` endpoints · RFC 9110 ProblemDetails · permissions/RBAC |
| Auth | ASP.NET Identity — cookie for browsers, bearer for other clients, selected per request |
| Data | PostgreSQL + EF Core · dedicated `Migration` worker (migrate + seed at startup) |
| Orchestration | .NET Aspire (`src/AppHost`) — Postgres · pgAdmin · Migration · Web API · React dev server |
| Tests | `tests/TestAppHost` real-PostgreSQL fixtures (Respawn) |
| Deployment | `deploy/` — Docker Compose behind Traefik, plus a reusable GitHub Actions workflow |

Angular, the old React app, SQLite, SQL Server and the non-Aspire and azd paths from the base
template have been removed.

## Quick start

```powershell
# Install the template from this repo
dotnet new install .

# Create a new project (CleanArchitecture → MyProject everywhere)
dotnet new ca-sln -o MyProject                       # React dashboard
dotnet new ca-sln -o MyApi --client-framework None   # Web API only
```

Then follow the generated project's `README.md` for first-time Postgres setup and
`dotnet run --project ./src/AppHost`.

## Documentation

See **[TEMPLATE_GUIDE.md](TEMPLATE_GUIDE.md)** for:

- **Using** the template — prerequisites, install/instantiate, run, regenerate the Orval client, where to add a feature.
- **Updating** this template from the upstream base (`git fetch upstream`, merge/cherry-pick, per-area conflict policy).
- **Maintaining** the template — smoke-testing instantiation, genericization tokens, how the single stack is enforced in `template.json`.

## Credits

Built on the excellent [Clean Architecture Solution Template](https://github.com/jasontaylordev/CleanArchitecture)
by [Jason Taylor](https://github.com/jasontaylordev).
