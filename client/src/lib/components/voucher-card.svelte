<script lang="ts">
	import { Button, Card, Input, Label } from "@kayord/ui";
	import { VoucherDiscountMode, VoucherRedemptionKind, type BookingVoucherDTO, type PaymentVoucherRequest } from "$lib/api";
	import { formatCurrency, formatDate } from "$lib/booking/format";
	import { voucherRequest } from "$lib/booking/payments";

	let {
		voucher,
		bookingId,
		busy = false,
		disabled = false,
		onredeem,
	}: {
		voucher: BookingVoucherDTO;
		bookingId: number;
		busy?: boolean;
		disabled?: boolean;
		onredeem: (_request: PaymentVoucherRequest) => Promise<void>;
	} = $props();
	let targetKey = $state("");
	let quantity = $state<number | undefined>(1);
	const entitlement = $derived(voucher.redemptionKind === VoucherRedemptionKind.Entitlement);
	const target = $derived(voucher.targets.find((item) => String(voucher.isExtra ? item.extraId : item.slotContractBookingId) === targetKey));
	const maxQuantity = $derived(target ? Math.min(target.unitsAvailable, Math.floor(voucher.amountRemaining)) : 0);
	const request = $derived(voucherRequest(bookingId, voucher, target, quantity ?? 0));
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{voucher.name}</Card.Title>
		<Card.Description>{voucher.description}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-3">
		{#if entitlement}
			<p>{voucher.amountRemaining} {voucher.isExtra ? "extra units" : "rounds"} remaining</p>
		{:else if voucher.redemptionKind === VoucherRedemptionKind.Credit}
			<p>Credit remaining: {formatCurrency(voucher.amountRemaining)}</p>
		{:else if voucher.discountMode === VoucherDiscountMode.Percentage}
			<p>{voucher.discountValue}% discount{voucher.maxDiscountAmount != null ? ` · Maximum ${formatCurrency(voucher.maxDiscountAmount)}` : ""}</p>
		{:else}
			<p>Fixed discount: {formatCurrency(voucher.discountValue)}</p>
		{/if}
		<p class="text-muted-foreground text-sm">Expires {formatDate(voucher.expiryDate)} · {voucher.isExtra ? "Extras only" : "Rounds only"}</p>
		{#if voucher.isEligible}
			<p>Eligible subtotal: {formatCurrency(voucher.eligibleAmount)}</p>
			<p>{entitlement ? "Value for one available unit" : "Payment value"}: {formatCurrency(voucher.paymentValue)}</p>
			{#if entitlement}
				<Label for={`target-${voucher.grantId}`}>Choose an eligible {voucher.isExtra ? "extra" : "round"}</Label>
				<select id={`target-${voucher.grantId}`} class="bg-background w-full rounded-md border p-2" bind:value={targetKey} disabled={busy || disabled}>
					<option value="">Select an item</option>
					{#each voucher.targets as item (voucher.isExtra ? item.extraId : item.slotContractBookingId)}
						<option value={String(voucher.isExtra ? item.extraId : item.slotContractBookingId)} disabled={item.unitsAvailable < 1}>
							{item.name} · {formatCurrency(item.unitPrice)} · {item.unitsAvailable} available
						</option>
					{/each}
				</select>
				<Label for={`quantity-${voucher.grantId}`}>Quantity (maximum {maxQuantity})</Label>
				<Input
					id={`quantity-${voucher.grantId}`}
					type="number"
					min="1"
					max={maxQuantity}
					step="1"
					bind:value={quantity}
					disabled={busy || disabled || !target}
				/>
				<p class="text-muted-foreground text-sm">The backend determines the applied value, capped at the eligible unpaid balance.</p>
			{:else}
				<p class="text-muted-foreground text-sm">Applies to the eligible subtotal; no item selection is needed.</p>
			{/if}
		{:else}
			<p role="status">Not eligible: {voucher.ineligibleReason ?? "This voucher cannot be used for this booking."}</p>
		{/if}
	</Card.Content>
	<Card.Footer>
		<Button
			disabled={busy || disabled || !request}
			onclick={async () => {
				if (request && !busy && !disabled) await onredeem(request);
			}}>Redeem voucher</Button
		>
	</Card.Footer>
</Card.Root>
