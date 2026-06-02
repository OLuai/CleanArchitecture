import { useState } from 'react';
import { createFileRoute } from '@tanstack/react-router';
import { MinusIcon, PlusIcon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';

export const Route = createFileRoute('/counter')({
  component: CounterPage,
});

function CounterPage() {
  const [count, setCount] = useState(0);

  return (
    <div className="mx-auto max-w-md">
      <Card>
        <CardHeader>
          <CardTitle>Counter</CardTitle>
          <CardDescription>A simple client-side stateful component.</CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-center gap-4">
          <Button
            variant="outline"
            size="icon"
            aria-label="Decrement"
            onClick={() => setCount((c) => c - 1)}
          >
            <MinusIcon />
          </Button>
          <span
            className="min-w-16 text-center text-4xl font-bold tabular-nums"
            aria-live="polite"
          >
            {count}
          </span>
          <Button
            size="icon"
            aria-label="Increment"
            onClick={() => setCount((c) => c + 1)}
          >
            <PlusIcon />
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
