import { useMemo, useState } from 'react';
import { createFileRoute, redirect } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import { MoreHorizontalIcon, PlusIcon, SettingsIcon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import { ensureAuthenticated } from '@/lib/auth';
import { Can, ensurePermission, Permission } from '@/lib/permissions';
import {
  useGetTodoLists,
  useCreateTodoList,
  useUpdateTodoList,
  useDeleteTodoList,
  getGetTodoListsQueryKey,
} from '@/api/generated/todo-lists/todo-lists';
import {
  useCreateTodoItem,
  useUpdateTodoItem,
  useDeleteTodoItem,
  useUpdateTodoItemDetail,
} from '@/api/generated/todo-items/todo-items';
import type {
  ColourDto,
  LookupDto,
  TodoItemDto,
  TodoListDto,
  TodosVm,
} from '@/api/generated/model';

export const Route = createFileRoute('/todo')({
  beforeLoad: async ({ context, location }) => {
    try {
      await ensureAuthenticated(context.queryClient);
    } catch {
      throw redirect({ to: '/login', search: { returnUrl: location.pathname } });
    }
    await ensurePermission(context.queryClient, Permission.TodoListsView);
  },
  component: TodoPage,
});

type Id = number | string;

function TodoPage() {
  const queryClient = useQueryClient();
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: getGetTodoListsQueryKey() });
  const mutationOpts = { mutation: { onSuccess: invalidate } };

  const { data, isLoading } = useGetTodoLists();
  // The mutator throws on non-2xx, so a resolved query is always the success payload.
  const vm = data?.data as TodosVm | undefined;
  const lists = useMemo<TodoListDto[]>(() => vm?.lists ?? [], [vm]);
  const colours = useMemo<ColourDto[]>(() => vm?.colours ?? [], [vm]);
  const priorityLevels = useMemo<LookupDto[]>(() => vm?.priorityLevels ?? [], [vm]);

  const createList = useCreateTodoList(mutationOpts);
  const updateList = useUpdateTodoList(mutationOpts);
  const deleteList = useDeleteTodoList(mutationOpts);
  const createItem = useCreateTodoItem(mutationOpts);
  const updateItem = useUpdateTodoItem(mutationOpts);
  const deleteItem = useDeleteTodoItem(mutationOpts);
  const updateItemDetail = useUpdateTodoItemDetail(mutationOpts);

  const [selectedListId, setSelectedListId] = useState<Id | null>(null);
  const [addingItem, setAddingItem] = useState(false);
  const [newItemTitle, setNewItemTitle] = useState('');
  const [editingItemId, setEditingItemId] = useState<Id | null>(null);
  const [editValue, setEditValue] = useState('');

  // Fall back to the first list until the user explicitly picks one.
  const currentListId = selectedListId ?? lists[0]?.id ?? null;
  const selectedList = lists.find((l) => l.id === currentListId) ?? null;
  const remaining = (list: TodoListDto) =>
    (list.items ?? []).filter((i) => !i.done).length;

  const selectList = (id: Id | null) => {
    setSelectedListId(id);
    setAddingItem(false);
    setNewItemTitle('');
    setEditingItemId(null);
  };

  // ── New list dialog ────────────────────────────────────────────────────────
  const [newListOpen, setNewListOpen] = useState(false);
  const [newListTitle, setNewListTitle] = useState('');
  const [newListColour, setNewListColour] = useState('');
  const [newListError, setNewListError] = useState('');

  const openNewList = () => {
    setNewListTitle('');
    setNewListColour(colours[0]?.code ?? '');
    setNewListError('');
    setNewListOpen(true);
  };

  const commitNewList = async () => {
    if (!newListTitle.trim()) {
      setNewListError('Title is required.');
      return;
    }
    const created = await createList.mutateAsync({
      data: { title: newListTitle.trim(), colour: newListColour },
    });
    setSelectedListId(created.data as Id);
    setNewListOpen(false);
  };

  // ── List options / delete dialogs ────────────────────────────────────────────
  const [optionsOpen, setOptionsOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [optTitle, setOptTitle] = useState('');
  const [optColour, setOptColour] = useState('');

  const openOptions = () => {
    if (!selectedList) return;
    setOptTitle(selectedList.title ?? '');
    setOptColour(selectedList.colour ?? colours[0]?.code ?? '');
    setOptionsOpen(true);
  };

  const commitOptions = async () => {
    if (!selectedList?.id) return;
    await updateList.mutateAsync({
      id: selectedList.id,
      data: { id: selectedList.id, title: optTitle, colour: optColour },
    });
    setOptionsOpen(false);
  };

  const commitDeleteList = async () => {
    if (!selectedList?.id) return;
    await deleteList.mutateAsync({ id: selectedList.id });
    setDeleteOpen(false);
    setOptionsOpen(false);
    setSelectedListId(null);
  };

  // ── Item editing ─────────────────────────────────────────────────────────────
  const commitNewItem = async () => {
    setAddingItem(false);
    const title = newItemTitle.trim();
    setNewItemTitle('');
    if (!title || currentListId == null) return;
    await createItem.mutateAsync({ data: { listId: currentListId, title } });
  };

  const toggleDone = async (item: TodoItemDto, done: boolean) => {
    if (item.id == null) return;
    await updateItem.mutateAsync({
      id: item.id,
      data: { id: item.id, title: item.title, done },
    });
  };

  const startEdit = (item: TodoItemDto) => {
    setEditingItemId(item.id ?? null);
    setEditValue(item.title ?? '');
  };

  const commitEdit = async (item: TodoItemDto) => {
    const title = editValue.trim();
    setEditingItemId(null);
    if (item.id == null) return;
    if (!title) {
      await deleteItem.mutateAsync({ id: item.id });
      return;
    }
    if (title === item.title) return;
    await updateItem.mutateAsync({
      id: item.id,
      data: { id: item.id, title, done: item.done },
    });
  };

  // ── Item details dialog ──────────────────────────────────────────────────────
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [detailItem, setDetailItem] = useState<TodoItemDto | null>(null);
  const [detailListId, setDetailListId] = useState<Id | undefined>(undefined);
  const [detailPriority, setDetailPriority] = useState<Id | undefined>(undefined);
  const [detailNote, setDetailNote] = useState('');

  const openDetails = (item: TodoItemDto) => {
    setDetailItem(item);
    setDetailListId(item.listId);
    setDetailPriority(item.priority);
    setDetailNote(item.note ?? '');
    setDetailsOpen(true);
  };

  const commitDetails = async () => {
    if (!detailItem?.id) return;
    await updateItemDetail.mutateAsync({
      id: detailItem.id,
      data: {
        id: detailItem.id,
        listId: detailListId,
        priority:
          detailPriority == null ? undefined : (Number(detailPriority) as number),
        note: detailNote,
      },
    });
    setDetailsOpen(false);
  };

  const removeDetailItem = async () => {
    if (!detailItem?.id) return;
    await deleteItem.mutateAsync({ id: detailItem.id });
    setDetailsOpen(false);
  };

  if (isLoading) {
    return <p className="text-muted-foreground">Loading…</p>;
  }

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-3xl font-bold tracking-tight">Tasks</h1>
        <p className="text-muted-foreground">Manage your todo lists and tasks.</p>
      </div>

      <div className="grid gap-6 md:grid-cols-[260px_1fr]">
        {/* Sidebar */}
        <aside className="space-y-2">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold">Lists</h2>
            <Can permission={Permission.TodoListsCreate}>
              <Button variant="ghost" size="icon" aria-label="New list" onClick={openNewList}>
                <PlusIcon />
              </Button>
            </Can>
          </div>
          <ul className="space-y-1">
            {lists.map((list) => (
              <li key={String(list.id)}>
                <button
                  type="button"
                  onClick={() => selectList(list.id ?? null)}
                  className={cn(
                    'flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-accent',
                    selectedList?.id === list.id && 'bg-accent text-accent-foreground'
                  )}
                >
                  <span
                    aria-hidden
                    className="size-2.5 rounded-full"
                    style={{ background: list.colour ?? undefined }}
                  />
                  <span className="flex-1 truncate">{list.title}</span>
                  <span className="text-muted-foreground text-xs">{remaining(list)}</span>
                </button>
              </li>
            ))}
            {lists.length === 0 && (
              <li className="text-muted-foreground px-2 py-1.5 text-sm">No lists yet.</li>
            )}
          </ul>
        </aside>

        {/* Items */}
        {selectedList && (
          <section className="space-y-2">
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold" style={{ color: selectedList.colour ?? undefined }}>
                {selectedList.title}
              </h2>
              <Button variant="ghost" size="icon" aria-label="List options" onClick={openOptions}>
                <SettingsIcon />
              </Button>
            </div>

            <ul className="divide-y rounded-md border">
              {(selectedList.items ?? []).map((item) => (
                <li key={String(item.id)} className="flex items-center gap-3 px-3 py-2">
                  <Checkbox
                    checked={!!item.done}
                    onCheckedChange={(v) => toggleDone(item, v === true)}
                  />
                  {editingItemId === item.id ? (
                    <Input
                      autoFocus
                      className="h-8"
                      maxLength={200}
                      value={editValue}
                      onChange={(e) => setEditValue(e.target.value)}
                      onBlur={() => commitEdit(item)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') (e.target as HTMLInputElement).blur();
                        if (e.key === 'Escape') setEditingItemId(null);
                      }}
                    />
                  ) : (
                    <button
                      type="button"
                      onClick={() => startEdit(item)}
                      className={cn(
                        'flex-1 text-left text-sm',
                        item.done && 'text-muted-foreground line-through'
                      )}
                    >
                      {item.title}
                    </button>
                  )}
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Item details"
                    onClick={() => openDetails(item)}
                  >
                    <MoreHorizontalIcon />
                  </Button>
                </li>
              ))}

              <li className="flex items-center gap-3 px-3 py-2">
                <Checkbox disabled />
                {addingItem ? (
                  <Input
                    autoFocus
                    className="h-8"
                    maxLength={200}
                    placeholder="New task…"
                    value={newItemTitle}
                    onChange={(e) => setNewItemTitle(e.target.value)}
                    onBlur={commitNewItem}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') commitNewItem();
                      if (e.key === 'Escape') {
                        setAddingItem(false);
                        setNewItemTitle('');
                      }
                    }}
                  />
                ) : (
                  <button
                    type="button"
                    onClick={() => setAddingItem(true)}
                    className="text-muted-foreground flex-1 text-left text-sm"
                  >
                    New task…
                  </button>
                )}
              </li>
            </ul>
          </section>
        )}
      </div>

      {/* New list dialog */}
      <Dialog open={newListOpen} onOpenChange={setNewListOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New list</DialogTitle>
            <DialogDescription>Create a new todo list.</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="newListTitle">Title</Label>
              <Input
                id="newListTitle"
                autoFocus
                maxLength={200}
                placeholder="List title…"
                value={newListTitle}
                onChange={(e) => setNewListTitle(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && commitNewList()}
                aria-invalid={newListError ? true : undefined}
              />
              {newListError && <p className="text-destructive text-sm">{newListError}</p>}
            </div>
            <ColourPicker colours={colours} value={newListColour} onChange={setNewListColour} />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setNewListOpen(false)}>
              Cancel
            </Button>
            <Button onClick={commitNewList} disabled={createList.isPending}>
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* List options dialog */}
      <Dialog open={optionsOpen} onOpenChange={setOptionsOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>List options</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="optTitle">Title</Label>
              <Input
                id="optTitle"
                maxLength={200}
                value={optTitle}
                onChange={(e) => setOptTitle(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && commitOptions()}
              />
            </div>
            <ColourPicker colours={colours} value={optColour} onChange={setOptColour} />
          </div>
          <DialogFooter className="sm:justify-between">
            <Button variant="destructive" onClick={() => setDeleteOpen(true)}>
              Delete
            </Button>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setOptionsOpen(false)}>
                Cancel
              </Button>
              <Button onClick={commitOptions} disabled={updateList.isPending}>
                Update
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete list confirm */}
      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete &ldquo;{selectedList?.title}&rdquo;?</DialogTitle>
            <DialogDescription>All items will be permanently deleted.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteOpen(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={commitDeleteList} disabled={deleteList.isPending}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Item details dialog */}
      <Dialog open={detailsOpen} onOpenChange={setDetailsOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Item details</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>List</Label>
              <Select
                value={detailListId == null ? undefined : String(detailListId)}
                onValueChange={(v) => setDetailListId(v)}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select a list" />
                </SelectTrigger>
                <SelectContent>
                  {lists.map((list) => (
                    <SelectItem key={String(list.id)} value={String(list.id)}>
                      {list.title}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Priority</Label>
              <Select
                value={detailPriority == null ? undefined : String(detailPriority)}
                onValueChange={(v) => setDetailPriority(v)}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select a priority" />
                </SelectTrigger>
                <SelectContent>
                  {priorityLevels.map((level) => (
                    <SelectItem key={String(level.id)} value={String(level.id)}>
                      {level.title}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="detailNote">Note</Label>
              <Textarea
                id="detailNote"
                rows={3}
                value={detailNote}
                onChange={(e) => setDetailNote(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter className="sm:justify-between">
            <Button variant="destructive" onClick={removeDetailItem}>
              Delete
            </Button>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setDetailsOpen(false)}>
                Cancel
              </Button>
              <Button onClick={commitDetails} disabled={updateItemDetail.isPending}>
                Update
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function ColourPicker({
  colours,
  value,
  onChange,
}: {
  colours: ColourDto[];
  value: string;
  onChange: (code: string) => void;
}) {
  return (
    <div className="space-y-2">
      <Label>Colour</Label>
      <div className="flex flex-wrap gap-2">
        {colours.map((c) => (
          <button
            key={c.code}
            type="button"
            aria-label={c.name}
            onClick={() => onChange(c.code ?? '')}
            className={cn(
              'size-7 rounded-full border-2 transition-transform',
              value === c.code ? 'border-foreground scale-110' : 'border-transparent'
            )}
            style={{ background: c.code }}
          />
        ))}
      </div>
    </div>
  );
}
