<script lang="ts">
	import "../layout.css";
	import favicon from "$lib/assets/favicon.svg";
	import { ModeWatcher } from "mode-watcher";
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import { browser } from "$app/environment";
	import { Toaster, toast } from "svelte-sonner";
	import { setUserContext } from "$lib/auth";

	let { children, data } = $props();

	// Push the signed-in user (non-secret) into context for the whole app.
	// svelte-ignore state_referenced_locally
	setUserContext(data.user);

	// TEMPORARY BRIDGE: keeps not-yet-migrated pages working while we convert
	// them to remote functions. Removed once all tanstack-query usage is gone.
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: {
				enabled: browser,
			},
		},
	});

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
<QueryClientProvider client={queryClient}>
	{@render children?.()}
</QueryClientProvider>
