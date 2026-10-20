<script lang="ts">
	import { browser } from "$app/env";
	import { Alert, Badge, Button, Card, Dialog } from "@kayord/ui";
	import { TicketIcon } from "@lucide/svelte";
	import { createMutation, createQuery, useQueryClient } from "@tanstack/svelte-query";
	import { onMount } from "svelte";
	import { walletTransfer, walletVouchers } from "$lib/api/remote/wallet.remote";
	import { VoucherDiscountMode, VoucherRedemptionKind, type WalletVoucherDTO } from "$lib/api/generated";
	import { formatCurrency, formatDate } from "$lib/booking/format";
	import { groupVouchers, voucherStatus } from "$lib/wallet/vouchers";

	import VoucherSend from "$lib/components/voucher-send.svelte";

	let sendTarget = $state.raw<WalletVoucherDTO>();
	let transferComplete = $state(false);
	const queryClient = useQueryClient();
	const transfer = createMutation(() => ({
		mutationFn: (variables: { grantId: string; body: { recipient: string } }) => walletTransfer(variables),
		onSuccess: async () => {
			await query.refetch();
			await queryClient.invalidateQueries({ queryKey: ["booking-vouchers"] });
		},
	}));
	async function sendVoucher(body: { recipient: string }) {
		if (!sendTarget || transfer.isPending) return;
		await transfer.mutateAsync({ grantId: sendTarget.grantId, body });
		sendTarget = undefined;
		transferComplete = true;
	}
	let now = $state(Date.now());
	onMount(() => {
		const timer = setInterval(() => (now = Date.now()), 60000);
		return () => clearInterval(timer);
	});
	const query = createQuery(() => ({
		queryKey: ["wallet-vouchers"],
		enabled: browser,
		retry: false,
		queryFn: async () => {
			const request = walletVouchers();
			await request.refresh();
			return await request;
		},
	}));
	const groups = $derived(groupVouchers(query.data ?? []));
</script>

<section class="mt-6" aria-labelledby="vouchers-heading">
	<div class="mb-4 flex items-center justify-between gap-4">
		<h2 id="vouchers-heading" class="text-xl font-semibold">My vouchers</h2>
		<Button variant="outline" size="sm" disabled={query.isFetching} onclick={() => query.refetch()}>Refresh</Button>
	</div>
	{#if transferComplete}<p role="status" class="mb-4 text-sm">Voucher sent successfully.</p>{/if}
	{#if query.isPending}
		<p role="status" class="text-muted-foreground">Loading your vouchers...</p>
	{:else if query.isError}
		<Alert.Root variant="destructive">
			<Alert.Title>Unable to load vouchers</Alert.Title>
			<Alert.Description>Please try refreshing your vouchers.</Alert.Description>
		</Alert.Root>
	{:else if groups.length === 0}
		<Card.Root>
			<Card.Content class="flex flex-col items-center gap-3 py-10 text-center">
				<TicketIcon class="text-muted-foreground size-10" />
				<h3 class="font-semibold">No vouchers yet</h3>
				<p class="text-muted-foreground text-sm">Vouchers issued to you will appear here.</p>
			</Card.Content>
		</Card.Root>
	{:else}
		<p class="text-muted-foreground mb-4 text-sm">Use available vouchers when paying for an eligible booking.</p>
		<div class="grid items-start gap-4 md:grid-cols-2 xl:grid-cols-3">
			{#each groups as group (group.key)}
				{@const voucher = group.voucher}
				{@const entitlement = voucher.redemptionKind === VoucherRedemptionKind.Entitlement}
				{@const available = group.grants.filter((grant) => voucherStatus(grant, now) === "Available").reduce((sum, grant) => sum + grant.amountRemaining, 0)}
				<Card.Root class="overflow-hidden">
					<Card.Header>
						<div class="mb-2 flex items-center justify-between gap-2">
							<div class="bg-primary/10 text-primary rounded-lg p-2"><TicketIcon class="size-5" /></div>
							<Badge variant="secondary">{voucher.redemptionKind}</Badge>
						</div>
						<Card.Title>{voucher.name}</Card.Title>
						<Card.Description>{voucher.description}</Card.Description>
					</Card.Header>
					<Card.Content class="space-y-4">
						{#if entitlement}
							<p class="text-2xl font-bold">{available} {voucher.isExtra ? "extra units" : "rounds"} available</p>
						{:else if voucher.redemptionKind === VoucherRedemptionKind.Credit}
							<p class="text-2xl font-bold">
								{voucher.currency === "ZAR" ? formatCurrency(voucher.amountRemaining) : `${voucher.currency} ${voucher.amountRemaining.toFixed(2)}`} remaining
							</p>
						{:else}
							<p class="text-2xl font-bold">
								{voucher.discountMode === VoucherDiscountMode.Percentage
									? `${voucher.discountValue}% discount`
									: `${formatCurrency(voucher.discountValue ?? 0)} discount`}
							</p>
							{#if voucher.maxDiscountAmount != null}<p class="text-muted-foreground text-sm">
									Maximum discount: {formatCurrency(voucher.maxDiscountAmount)}
								</p>{/if}
						{/if}
						<dl class="space-y-2 text-sm">
							<div>
								<dt class="font-medium">Valid at</dt>
								<dd class="text-muted-foreground">{voucher.facilityNames.join(", ") || "No eligible facilities"}</dd>
							</div>
							<div>
								<dt class="font-medium">Can be used for</dt>
								<dd class="text-muted-foreground">
									{#if voucher.isExtra}
										Extras: {voucher.extraNames.join(", ") || "No eligible extras"}
									{:else}
										Games / rounds{voucher.gameTypes.length ? `: ${voucher.gameTypes.join(", ")}` : ""}
									{/if}
								</dd>
							</div>
						</dl>
						<p class="text-muted-foreground text-sm">
							{voucher.isExtra ? "Extras only" : "Rounds only"}{entitlement
								? ` · ${group.grants.length} voucher grant${group.grants.length === 1 ? "" : "s"}`
								: ""}
						</p>
						<ul class="space-y-3 border-t pt-4">
							{#each group.grants as grant (grant.grantId)}
								<li class="space-y-1 text-sm">
									<div class="flex flex-wrap items-center justify-between gap-2">
										<p>
											{grant.redemptionKind === VoucherRedemptionKind.Credit
												? `${grant.amountRemaining} ${grant.currency}`
												: `${grant.amountRemaining} ${entitlement ? (grant.isExtra ? "extra units" : "rounds") : "uses"}`} remaining
										</p>
										<Badge variant={voucherStatus(grant, now) === "Available" ? "default" : "secondary"}>{voucherStatus(grant, now)}</Badge>
									</div>
									<p class="text-muted-foreground">Valid from {formatDate(grant.grantedAt)} · Expires {formatDate(grant.expiryDate)}</p>
									{#if grant.canTransfer && grant.isWalletActive && grant.amountRemaining > 0 && new Date(grant.expiryDate).getTime() > now}
										<Button
											variant="outline"
											size="sm"
											onclick={() => {
												sendTarget = grant;
												transferComplete = false;
											}}>Send voucher</Button
										>
									{/if}
								</li>
							{/each}
						</ul>
					</Card.Content>
				</Card.Root>
			{/each}
		</div>
	{/if}
</section>

<Dialog.Root open={sendTarget !== undefined} onOpenChange={(open) => !open && !transfer.isPending && (sendTarget = undefined)}>
	<Dialog.Content class="sm:max-w-lg" showCloseButton={!transfer.isPending}>
		<Dialog.Header>
			<Dialog.Title>Send voucher</Dialog.Title>
			<Dialog.Description>Send {sendTarget?.name} to an email address or phone number.</Dialog.Description>
		</Dialog.Header>
		{#if sendTarget}
			<VoucherSend grant={sendTarget} onsend={sendVoucher} oncancel={() => (sendTarget = undefined)} />
		{/if}
	</Dialog.Content>
</Dialog.Root>
