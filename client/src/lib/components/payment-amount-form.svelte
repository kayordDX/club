<script lang="ts">
	import { Button, ToggleGroup } from "@kayord/ui";
	import { createAppForm } from "$lib/components/Form";
	import type { FacilityPaymentMethodsResponse } from "$lib/api";
	import { validatePaymentAmount } from "$lib/booking/payments";

	let {
		amountAvailable,
		methods,
		busy,
		onpay,
	}: { amountAvailable: number; methods: FacilityPaymentMethodsResponse[]; busy: boolean; onpay: (_provider: string, _amount: number) => Promise<void> } =
		$props();
	let provider = $state("");
	const form = createAppForm(() => ({
		defaultValues: { amount: amountAvailable.toFixed(2) },
		validators: {
			onChange: ({ value }) => validatePaymentAmount(value.amount, amountAvailable),
			onSubmit: ({ value }) => validatePaymentAmount(value.amount, amountAvailable),
		},
		onSubmit: async ({ value }) => {
			if (!busy && provider && !validatePaymentAmount(value.amount, amountAvailable)) await onpay(provider, Number(value.amount));
		},
	}));
</script>

<form
	class="space-y-4"
	onsubmit={(event) => {
		event.preventDefault();
		form.handleSubmit();
	}}
>
	<form.AppField
		name="amount"
		validators={{
			onChange: ({ value }) => validatePaymentAmount(value, amountAvailable),
			onSubmit: ({ value }) => validatePaymentAmount(value, amountAvailable),
		}}
	>
		{#snippet children(field)}
			<field.Input label="Payment amount" inputmode="decimal" disabled={busy} />
		{/snippet}
	</form.AppField>
	{#if methods.length}
		<p>Choose a payment method</p>
		<ToggleGroup.Root type="single" variant="outline" bind:value={provider} disabled={busy} class="flex flex-wrap">
			{#each methods as method (method.providerName)}
				<ToggleGroup.Item value={method.providerName}>{method.type}</ToggleGroup.Item>
			{/each}
		</ToggleGroup.Root>
	{:else}
		<p>No provider payment methods are available. You can still use an eligible voucher.</p>
	{/if}
	<form.Subscribe selector={(state) => ({ valid: state.isValid, submitting: state.isSubmitting })}>
		{#snippet children(state)}
			<Button type="submit" disabled={busy || state.submitting || !state.valid || !provider || amountAvailable <= 0}
				>{busy ? "Processing..." : "Pay now"}</Button
			>
		{/snippet}
	</form.Subscribe>
</form>
