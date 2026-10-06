<script lang="ts">
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import VouchersPage from "./+page.svelte";
	const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
</script>

<QueryClientProvider {client}><VouchersPage /></QueryClientProvider>
