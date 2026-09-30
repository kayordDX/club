<script lang="ts">
	import PageHeading from "$lib/components/PageHeading.svelte";
	import WalletVouchers from "$lib/components/wallet-vouchers.svelte";
	import { WalletIcon } from "@lucide/svelte";
</script>

<div class="m-4">
	<PageHeading title="Wallet" description="Your vouchers, credits and discounts" icon={WalletIcon} />
	<WalletVouchers />
</div>
