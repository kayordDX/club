<script lang="ts">
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import type { ComponentProps } from "svelte";
	import PaymentResultCard from "../payment-result-card.svelte";

	let props: ComponentProps<typeof PaymentResultCard> = $props();
	const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
</script>

<QueryClientProvider {client}><PaymentResultCard {...props} /></QueryClientProvider>
