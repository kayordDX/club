<script lang="ts">
	import type { AdminVoucherDTO, AdminVoucherIssueRequest } from "#lib/api/index.js";
	import VoucherSend from "#lib/components/voucher-send.svelte";

	let { voucher, onsend, oncancel }: { voucher: AdminVoucherDTO; onsend: (_body: AdminVoucherIssueRequest) => Promise<void>; oncancel: () => void } = $props();
</script>

<VoucherSend {voucher} {onsend} {oncancel} />
