# CleanArchitecture React Client

This project uses [Vite](https://vitejs.dev/) with React 19 and TypeScript, styled with
[Tailwind CSS](https://tailwindcss.com/) + [shadcn/ui](https://ui.shadcn.com/), and built on the
[TanStack](https://tanstack.com/) suite (Router, Query, Form, Table). The API client and its
TanStack Query hooks are generated from the backend OpenAPI document by [Orval](https://orval.dev/).

## Available Scripts

### `npm start`

Runs the app in development mode with hot module replacement.
Opens at [https://localhost:44447](https://localhost:44447).

The development server proxies API requests to the ASP.NET Core backend.

### `npm run build`

Builds the app for production to the `build` folder.
Optimizes the build for best performance.

### `npm run preview`

Previews the production build locally.

### `npm run lint`

Runs ESLint on the src directory.

### `npm run generate-api`

Regenerates the typed API client and TanStack Query hooks from `../wwwroot/openapi/v1.json` using
Orval (also runs automatically before `start`/`build`).

## Project Structure

- `src/` - React source code
- `src/main.tsx` - Application entry point (QueryClient + RouterProvider + ThemeProvider)
- `src/routes/` - File-based TanStack Router routes (`__root.tsx`, `index.tsx`, `login.tsx`, …)
- `src/routeTree.gen.ts` - Auto-generated route tree (do not edit)
- `src/components/ui/` - shadcn/ui components; `src/components/` - app components
- `src/api/generated/` - Orval-generated client + hooks (do not edit); `src/api/mutator/` - fetch mutator
- `src/lib/` - `utils.ts` (cn), `query-client.ts`, `auth.ts`
- `public/` - Static assets (favicon, manifest)
- `vite.config.ts` - Vite configuration with proxy settings
- `index.html` - HTML template

## Environment Variables

Vite environment variables must be prefixed with `VITE_` to be exposed to client code.

Example:
```
VITE_API_URL=https://api.example.com
```

Access in code:
```javascript
const apiUrl = import.meta.env.VITE_API_URL;
```

## HTTPS Configuration

The development server uses ASP.NET Core development certificates for HTTPS.
Run `npm start` to automatically set up certificates via `aspnetcore-https.js`.

## Learn More

- [Vite Documentation](https://vitejs.dev/)
- [React Documentation](https://react.dev/)
