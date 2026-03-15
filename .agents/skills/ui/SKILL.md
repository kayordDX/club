---
name: ui
description: >
    Use this skill when building UI using @kayord/ui (shadcn-svelte) components — buttons, dialogs,
    sheets, popovers, dropdowns, tooltips, forms, inputs, or selects. Covers import patterns,
    trigger snippets, child snippet composition, and the cn utility. Apply when adding or
    customizing any @kayord/ui / shadcn-svelte component in the frontend.
---

# @kayord/ui shadcn-svelte Components

> **Documentation:** [github](https://github.com/kayordDX/ui) based on [shadcn-svelte.com](https://www.shadcn-svelte.com/)

Use @kayord/ui shadcn-svelte components (bits-ui) for UI. Import with namespace pattern.

## Import Pattern

```svelte
<script lang="ts">
    import { Dialog } from "@kayord/ui";
    import { DropdownMenu} from "@kayord/ui";
    import { Tooltip } from "@kayord/ui";
    import { Button } from "@kayord/ui";
    import { Input } from "@kayord/ui";
    import { Card } from "@kayord/ui";
    
    // They can all be in single import example
    import { Dialog, Button, Card } from "@kayord/ui";
</script>
```

## Trigger Components - Child Snippet Pattern

When using trigger components with custom elements like Button, **always use the `child` snippet pattern**:

```svelte
<!-- ✅ Correct: Single tab stop, proper accessibility -->
<Tooltip.Root>
    <Tooltip.Trigger>
        {#snippet child({ props })}
            <Button {...props} variant="ghost" size="icon">
                <Icon />
            </Button>
        {/snippet}
    </Tooltip.Trigger>
    <Tooltip.Content>Tooltip text</Tooltip.Content>
</Tooltip.Root>
```

### Why This Pattern?

- **Single Tab Stop**: Creates only one focusable element
- **Proper Props Delegation**: ARIA attributes pass through correctly
- **Accessibility**: Maintains keyboard navigation
- **Official Pattern**: Documented shadcn-svelte/bits-ui pattern

### Wrong Patterns

```svelte
<!-- ❌ Wrong: Creates two focusable elements (double-tab issue) -->
<Tooltip.Trigger>
    <Button>Content</Button>
</Tooltip.Trigger>

<!-- ❌ Wrong: Manual styling replicates button styles -->
<Tooltip.Trigger class="hover:bg-accent inline-flex...">
    <Icon />
</Tooltip.Trigger>
```

### Apply to All Triggers

```svelte
<!-- DropdownMenu -->
<DropdownMenu.Trigger>
    {#snippet child({ props })}
        <Button {...props} variant="outline">
            Open Menu
            <ChevronDown />
        </Button>
    {/snippet}
</DropdownMenu.Trigger>

<!-- Popover -->
<Popover.Trigger>
    {#snippet child({ props })}
        <Button {...props} variant="outline" class="w-70">
            Select Date
            <CalendarIcon />
        </Button>
    {/snippet}
</Popover.Trigger>

<!-- Dialog -->
<Dialog.Trigger>
    {#snippet child({ props })}
        <Button {...props}>Open Dialog</Button>
    {/snippet}
</Dialog.Trigger>
```

## Dialog Pattern

```svelte
<script lang="ts">
    import * as Dialog from '$comp/ui/dialog';
    import { Button } from '$comp/ui/button';

    let openCreateDialog = $state(false);
</script>

<Button onclick={() => (openCreateDialog = true)}>Create</Button>

{#if openCreateDialog}
    <Dialog.Root bind:open={openCreateDialog}>
        <Dialog.Content>
            <Dialog.Header>
                <Dialog.Title>Create Organization</Dialog.Title>
                <Dialog.Description>
                    Add a new organization to your account.
                </Dialog.Description>
            </Dialog.Header>

            <!-- Form content -->

            <Dialog.Footer>
                <Button variant="outline" onclick={() => (openCreateDialog = false)}>
                    Cancel
                </Button>
                <Button type="submit">Create</Button>
            </Dialog.Footer>
        </Dialog.Content>
    </Dialog.Root>
{/if}
```

## Dialog Naming Convention

- Use `open[ComponentName]Dialog` pattern
- Avoid generic names like `showDialog` or `isOpen`

```svelte
<script lang="ts">
    let openSuspendOrganizationDialog = $state(false);
    let openMarkStackDiscardedDialog = $state(false);
    let openInviteUserDialog = $state(false);
</script>
```

## DropdownMenu with Options

```svelte
<script lang="ts">
    import * as DropdownMenu from '$comp/ui/dropdown-menu';
    import { statusOptions } from './options';
</script>

<DropdownMenu.Root>
    <DropdownMenu.Trigger>
        {#snippet child({ props })}
            <Button {...props} variant="outline">
                Select Status
            </Button>
        {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content>
        {#each statusOptions as option}
            <DropdownMenu.Item onclick={() => handleSelect(option.value)}>
                {option.label}
            </DropdownMenu.Item>
        {/each}
    </DropdownMenu.Content>
</DropdownMenu.Root>
```

## Options File Pattern

```typescript
// options.ts
import type { DropdownItem } from "$shared/types";

export enum Status {
    Active = "active",
    Inactive = "inactive",
    Pending = "pending",
}

export const statusOptions: DropdownItem<Status>[] = [
    { value: Status.Active, label: "Active" },
    { value: Status.Inactive, label: "Inactive" },
    { value: Status.Pending, label: "Pending" },
];
```

## Sheet (Slide-out Panel)

```svelte
<Sheet.Root bind:open={openFiltersSheet}>
    <Sheet.Content side="right">
        <Sheet.Header>
            <Sheet.Title>Filters</Sheet.Title>
        </Sheet.Header>

        <!-- Filter controls -->

        <Sheet.Footer>
            <Button onclick={applyFilters}>Apply</Button>
        </Sheet.Footer>
    </Sheet.Content>
</Sheet.Root>
```

## Class Merging with Array Syntax

Use Svelte array syntax for conditional classes (NOT cn utility):

```svelte
<!-- ✅ Preferred: Array syntax -->
<Button class={['w-full', isActive && 'bg-primary']}>
    Click me
</Button>

<div class={['flex items-center', expanded && 'bg-muted', className]}>
    Content
</div>

<!-- ❌ Avoid: cn utility (older pattern) -->
<Button class={cn('w-full', isActive && 'bg-primary')}>
```

## Navigation Preference

Prefer `href` navigation over `onclick`/`goto`:

```svelte
<!-- ✅ Preferred: Native navigation -->
<Button href="/organizations/new">Create</Button>

<!-- Use onclick only when navigation logic required -->
<Button onclick={async () => {
    await saveData();
    goto('/success');
}}>
    Save and Continue
</Button>
```

## Design Principles

- **Keep it simple** - Avoid unnecessary visual complexity or feature bloat
- **Use semantic Tailwind CSS** - Leverage utility classes for layout and styling
- **Maintain accessibility** - Ensure keyboard navigation and ARIA attributes work correctly
- **Responsive first** - Design for mobile, then enhance for larger screens

## Form Patterns

### Search Input with Button

```svelte
<form onsubmit={handleSearch}>
    <div class="relative">
        <SearchIcon class="absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2" />
        <Input
            type="text"
            placeholder="Search..."
            bind:value={searchQuery}
            class="pl-12"
        />
        <Button type="submit" class="absolute right-2 top-1/2 -translate-y-1/2">
            Search
        </Button>
    </div>
</form>
```
## API Data Fetching

Use TanStack Query hooks generated by Orval for fetching data:

### Basic Query Usage

```svelte
<script lang="ts">
    import { createUserGetById } from "$lib/api";
    
    const query = createUserGetById(() => ({ id: userId }));
    const user = $derived(query.data);
</script>
```

### Using Query Wrapper Component

```svelte
<Query {query} emptyText="No data found">
    <!-- Content renders only when data exists -->
    <div>{query.data.name}</div>
</Query>
```

### Mutation for Create/Update

```svelte
<script lang="ts">
    import { createUserUpdate } from "$lib/api";
    
    const updateMut = createUserUpdate();
    
    const handleSave = async () => {
        await updateMut.mutateAsync({ id: userId, name: newName });
    };
</script>

<Button onclick={handleSave}>
    {updateMut.isPending ? "Saving..." : "Save"}
</Button>
```
