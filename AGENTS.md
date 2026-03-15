# AGENTS.md - Coding Agent Instructions

## Project Overview

- Monorepo with two main apps:
  - `api/` - .NET 10 FastEndpoints backend in `api/src/online/`
  - `client/` - SvelteKit 5 frontend with TypeScript in `client/src/`
- Purpose: public booking and member login system with Google OAuth authentication

## Available Skills

- `ui` - Use for `@kayord/ui` / shadcn-svelte components, trigger patterns, forms, inputs, dialogs, dropdowns, and related frontend UI composition.
- `api` - Use for API and backend work including FastEndpoints, endpoint structure, backend conventions, service registration, EF patterns, and API debugging in `api/src/online/Features/`.
- `svelte-core-bestpractices` - Use for modern Svelte 5 patterns, reactivity, composition, styling, and performance guidance.
- `svelte-code-writer` - Use for Svelte documentation lookup and code analysis whenever creating, editing, or reviewing `.svelte`, `.svelte.ts`, or `.svelte.js` files.

Prefer these skills instead of duplicating their detailed instructions here.

## Commands

- See `api/src/online/online.csproj`, `api/tests/`, `client/package.json`, and VS Code tasks for available build, test, lint, preview, API generation, and migration commands.

## Code Style Guidelines

### General Formatting

- Tabs for frontend files; 4 spaces for backend files
- Line length: 100 characters
- Trailing commas: ES5 style
- Quotes: double quotes preferred
- File naming: kebab-case for files, PascalCase for classes and components

### Frontend (TypeScript/Svelte)

#### Svelte 5 Runes (Required)

```typescript
// State variables - $state() is globally available
let count = $state(0);
let user = $state<User | null>(null);

// Derived state
let doubled = $derived(count * 2);
let complexData = $derived.by(() => {
	// complex logic here
	return processedResult;
});

// Effects (use sparingly, prefer $derived)
$effect(() => {
	console.log(`Count is now ${count}`);
});

// Component props - new syntax
type Props = {
	title: string;
	optional?: boolean;
};
let { title, optional = false } = $props();
```

#### Event Handling

```typescript
// Modern event handling (not onclick|preventDefault)
<button onclick={(event) => {
	event.preventDefault();
	handleClick();
}}>Click me</button>

// Instead of legacy svelte:component
<MyComponent />
<props.icon />
```

#### Imports & Exports

```typescript
// Use const for functions instead of function expressions
export const fetchUserData = async (id: string) => {
	return await api.users.getById(id);
};

// Import organization
import type { ComponentType } from 'svelte';
import { page } from '$app/stores';
import { getUserData } from '$lib/api/generated';
import { Button } from '$lib/components';
```

#### API Usage

```typescript
// Use generated API client from src/lib/api/generated/
import { createQuery } from '@tanstack/svelte-query';
import { getUsersListUsersGet } from '$lib/api/generated';

const usersQuery = createQuery({
	queryKey: ['users'],
	queryFn: () => getUsersListUsersGet()
});
```

- Use generated API clients from `client/src/lib/api/generated/`
- Custom fetch or mutator logic lives in `client/src/lib/api/mutator/customInstance.svelte.ts`
- For UI component patterns with `@kayord/ui`, use the `ui` skill

### Backend (C# .NET)

- Use file-scoped namespaces
- Keep feature-based organization under `api/src/online/Features/`
- Keep entities singular and PascalCase
- Use DTOs from the dedicated `DTO/` area
- For endpoint structure, service registration, EF conventions, and backend implementation details, use the `api` skill

### Testing Conventions

- Frontend unit tests: `ComponentName.svelte.test.ts`, colocated with the component
- Frontend E2E tests: `feature-name.spec.ts` in `client/e2e/`
- Backend tests: `ClassNameTests.cs` using xUnit and arrange-act-assert

## Key Development Workflows

### Adding New API Endpoint

1. Create the endpoint in `api/src/online/Features/{FeatureName}/`
2. Register any required services in `Common/Extensions/`
3. Regenerate the frontend API client from `client/`
4. Update frontend usage as needed

For detailed backend endpoint patterns, use the `api` skill.

### Adding New Page or Component

1. Create the Svelte file in `client/src/routes/` or `client/src/lib/components/`
2. Use Svelte 5 runes syntax
3. Import generated API clients from `$lib/api/generated`
4. Add tests in the same area when appropriate

For Svelte implementation help, use `svelte-core-bestpractices` and `svelte-code-writer`.
For `@kayord/ui` component work, use the `ui` skill.

### Database Changes

1. Modify entities in `api/src/online/Entities/`
2. Add a migration with the normal EF workflow
3. Update the database

## Important Notes

- Never commit secrets; use `dotnet user-secrets` for local development
- API-first development: OpenAPI drives client generation
- Use tabs for frontend code and 4 spaces for backend code
- Svelte 5 runes are mandatory; avoid legacy Svelte patterns
- Prefer generated API clients over manual HTTP calls
- Prefer repo skills for detailed framework-specific guidance instead of repeating that guidance here

## Quick Reference

- API docs: `http://localhost:5000/scalar/v1`
- Aspire dashboard: `http://localhost:18888`
- Health checks, telemetry, and OpenAPI docs are enabled in development
- Run API client generation after API changes
- Use VS Code tasks or CLI for EF migrations
- Secrets management: `dotnet user-secrets set "Key" "Value"`

## Svelte and MCP Guidance

- If Svelte patterns are unclear, check existing code in the relevant feature directory first
- Use the Svelte MCP documentation tools when working on Svelte or SvelteKit tasks
- Use `list-sections` first, then fetch all relevant sections with `get-documentation`
- Use `svelte-autofixer` whenever writing Svelte code before returning it to the user
- Only generate a playground link after the user asks for one, and never for code already written into the project
