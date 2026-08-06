<script lang="ts">
	import "../layout.css";
	import favicon from "$lib/assets/favicon.svg";
	import { ModeWatcher } from "mode-watcher";
	import { Toaster, toast } from "svelte-sonner";
	import { setUserContext } from "$lib/auth";

	let { children, data } = $props();

	// Push the signed-in user (non-secret) into context for the whole app.
	// svelte-ignore state_referenced_locally
	setUserContext(data.user);

	const handleToasterClick = (event: MouseEvent) => {
		// Buttons (close/action) handle their own clicks
		if ((event.target as Element).closest("button")) return;
		const toastEl = (event.target as Element).closest("[data-sonner-toast]");
		if (!toastEl) return;
		// Match the clicked toast by its index in the active-toasts list
		const target = toast.getActiveToasts()[Number(toastEl.getAttribute("data-index"))];
		if (target) toast.dismiss(target.id);
	};
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<ModeWatcher defaultMode="dark" />
<Toaster onclick={handleToasterClick} />

<!--
	Async boundary for every page: remote-function data loads via
	{@const x = await fn()} suspend here (pending skeleton) and errors
	are caught here (failed + retry).
-->
<svelte:boundary>
	{#snippet failed(error, reset)}
		<div class="border-destructive/40 bg-destructive/5 m-4 flex flex-col items-center gap-3 rounded-lg border p-8 text-center">
			<p class="text-destructive font-medium">Something went wrong while loading.</p>
			<p class="text-muted-foreground text-sm">{(error as Error)?.message ?? String(error)}</p>
			<button class="text-primary hover:underline" onclick={reset}>Try again</button>
		</div>
	{/snippet}

	{#snippet pending()}
		<div class="m-4 space-y-3">
			<div class="bg-muted h-8 w-1/3 animate-pulse rounded"></div>
			<div class="bg-muted h-4 w-2/3 animate-pulse rounded"></div>
			<div class="bg-muted h-24 animate-pulse rounded"></div>
		</div>
	{/snippet}

	{@render children?.()}
</svelte:boundary>
