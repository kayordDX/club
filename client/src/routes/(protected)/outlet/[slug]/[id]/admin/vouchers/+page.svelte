<script lang="ts">
	import { browser } from "$app/environment";
	import { page } from "$app/state";
	import { createMutation, createQuery, useQueryClient } from "@tanstack/svelte-query";
	import { Alert, Badge, Button, Card, Dialog } from "@kayord/ui";
	import { PlusIcon, TicketIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { VoucherDiscountMode, VoucherRedemptionKind, type AdminVoucherCreateRequest } from "$lib/api";
	import { adminVoucherCreate, adminVoucherGetAll } from "$lib/api/remote/admin.remote";
	import AdminVoucherForm from "$lib/components/admin-voucher-form.svelte";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { formatCurrency } from "$lib/booking/format";

	const facilityId = $derived(Number(page.params.id) || 0);
	const queryClient = useQueryClient();
	let openCreateVoucherDialog = $state(false);
	const vouchers = createQuery(() => ({
		queryKey: ["admin-vouchers", facilityId],
		enabled: browser && facilityId > 0,
		queryFn: async () => {
			const request = adminVoucherGetAll(facilityId);
			await request.refresh();
			return await request;
		},
	}));
	const createVoucher = createMutation(() => ({
		mutationFn: adminVoucherCreate,
		onSuccess: (_voucher, variables) => {
			openCreateVoucherDialog = false;
			toast.success("Voucher created");
			void queryClient.invalidateQueries({ queryKey: ["admin-vouchers", variables.facilityId] });
		},
	}));
	const saveVoucher = async (body: Omit<AdminVoucherCreateRequest, "facilityId">) => {
		await createVoucher.mutateAsync({ facilityId, body });
	};
	const kindLabels = {
		[VoucherRedemptionKind.Entitlement]: "Entitlement",
		[VoucherRedemptionKind.Credit]: "Credit",
		[VoucherRedemptionKind.Discount]: "Discount",
	};
</script>

<div class="m-4 space-y-4">
	<PageHeading title="Vouchers" description="Create vouchers that can only be used at this facility." icon={TicketIcon} />
	<div class="flex flex-wrap items-center justify-between gap-3">
		<p class="text-muted-foreground text-sm">Voucher definitions set the benefit. Balances and expiry dates are assigned when issued to a wallet.</p>
		<Button onclick={() => (openCreateVoucherDialog = true)}><PlusIcon class="size-4" />New voucher</Button>
	</div>
	{#if vouchers.isPending}
		<p role="status" class="text-muted-foreground">Loading vouchers...</p>
	{:else if vouchers.isError}
		<Alert.Root variant="destructive">
			<Alert.Title>Unable to load vouchers</Alert.Title>
			<Alert.Description>Please try again.</Alert.Description>
			<Button variant="outline" disabled={vouchers.isFetching} onclick={() => vouchers.refetch()}>Retry</Button>
		</Alert.Root>
	{:else if !vouchers.data?.length}
		<Card.Root>
			<Card.Content class="space-y-2 py-10 text-center">
				<h2 class="font-semibold">No vouchers yet</h2>
				<p class="text-muted-foreground text-sm">Create your first voucher for this facility.</p>
			</Card.Content>
		</Card.Root>
	{:else}
		<div class="grid items-start gap-4 md:grid-cols-2 xl:grid-cols-3">
			{#each vouchers.data as voucher (voucher.id)}
				<Card.Root>
					<Card.Header>
						<div class="mb-2 flex flex-wrap gap-2">
							<Badge>{kindLabels[voucher.redemptionKind]}</Badge>
							<Badge variant="secondary">{voucher.isExtra ? "Extras only" : "Rounds only"}</Badge>
						</div>
						<Card.Title>{voucher.name}</Card.Title>
						{#if voucher.description}<Card.Description class="whitespace-pre-wrap">{voucher.description}</Card.Description>{/if}
					</Card.Header>
					<Card.Content class="space-y-2 text-sm">
						{#if voucher.redemptionKind === VoucherRedemptionKind.Entitlement}
							<p>Redeem for eligible {voucher.isExtra ? "extra units" : "rounds"}.</p>
						{:else if voucher.redemptionKind === VoucherRedemptionKind.Credit}
							<p>Spend wallet credit on eligible {voucher.isExtra ? "extras" : "rounds"}.</p>
						{:else}
							<p class="font-medium">
								{voucher.discountMode === VoucherDiscountMode.Percentage ? `${voucher.discountValue}%` : formatCurrency(voucher.discountValue)} discount
							</p>
							{#if voucher.maxDiscountAmount != null}<p>Maximum discount: {formatCurrency(voucher.maxDiscountAmount)}</p>{/if}
						{/if}
						<p class="text-muted-foreground">Valid at this facility only.</p>
					</Card.Content>
				</Card.Root>
			{/each}
		</div>
	{/if}
</div>

<Dialog.Root open={openCreateVoucherDialog} onOpenChange={(open) => !createVoucher.isPending && (openCreateVoucherDialog = open)}>
	<Dialog.Content class="max-h-[90dvh] overflow-y-auto sm:max-w-lg">
		<Dialog.Header>
			<Dialog.Title>New voucher</Dialog.Title>
			<Dialog.Description>This voucher will only be usable at the current facility.</Dialog.Description>
		</Dialog.Header>
		{#if openCreateVoucherDialog}
			{#key facilityId}
				<AdminVoucherForm onsave={saveVoucher} oncancel={() => (openCreateVoucherDialog = false)} />
			{/key}
		{/if}
	</Dialog.Content>
</Dialog.Root>
