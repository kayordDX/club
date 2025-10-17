<script lang="ts">
	import "../app.css";
	import favicon from "$lib/assets/favicon.svg";
	import { ModeWatcher } from "mode-watcher";
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import { browser } from "$app/environment";
	import { Toaster } from "svelte-sonner";
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";
	import Header from "$lib/components/Header/Header.svelte";

	const queryClient = new QueryClient({
		defaultOptions: {
			queries: {
				enabled: browser,
			},
		},
	});
	let { children } = $props();
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<ModeWatcher defaultMode="dark" />
<Toaster />
<QueryClientProvider client={queryClient}>
	<!-- <AuthCheck> -->
	<Header />
	{@render children?.()}
	<!-- </AuthCheck> -->
</QueryClientProvider>
