<script lang="ts">
	import "../layout.css";
	import favicon from "$lib/assets/favicon.svg";
	import { ModeWatcher } from "mode-watcher";
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import { browser } from "$app/environment";
	import { Toaster, toast } from "svelte-sonner";

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: {
				enabled: browser,
			},
		},
	});
	let { children } = $props();

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
