import { createFileRoute } from '@tanstack/react-router';

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';

export const Route = createFileRoute('/')({
  component: HomePage,
});

const stack = [
  ['ASP.NET Core + C#', 'Cross-platform server-side API'],
  ['React + Vite + TypeScript', 'Type-safe client-side application'],
  ['Tailwind CSS + shadcn/ui', 'Utility-first styling and accessible components'],
  ['TanStack Router / Query / Form / Table', 'Type-safe routing, data fetching, forms and tables'],
  ['Orval', 'Generated API client wrapped with TanStack Query'],
];

function HomePage() {
  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold tracking-tight">Welcome</h1>
        <p className="text-muted-foreground">
          A full-stack application with a React frontend and an ASP.NET Core backend,
          built with a modern, fully type-safe toolchain.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Technology stack</CardTitle>
          <CardDescription>What powers this application.</CardDescription>
        </CardHeader>
        <CardContent>
          <ul className="divide-y">
            {stack.map(([name, desc]) => (
              <li key={name} className="flex flex-col py-3 first:pt-0 last:pb-0">
                <span className="font-medium">{name}</span>
                <span className="text-muted-foreground text-sm">{desc}</span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
