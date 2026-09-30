<script lang="ts">
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import WalletVouchers from "../wallet-vouchers.svelte";
	const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
</script>

<QueryClientProvider {client}><WalletVouchers /></QueryClientProvider>
