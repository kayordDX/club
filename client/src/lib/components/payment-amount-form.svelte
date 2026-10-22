<script lang="ts">
	import { Button, ToggleGroup } from "@kayord/ui";
	import { createAppForm } from "#lib/components/Form/index.js";
	import type { FacilityPaymentMethodsResponse } from "#lib/api/index.js";
	import { validatePaymentAmount } from "#lib/booking/payments.js";

	let {
		amountAvailable,
		methods,
		busy,
		onpay,
		acceptInClub = false,
		allowPartialAmount = true,
	}: {
		amountAvailable: number;
		methods: FacilityPaymentMethodsResponse[];
		busy: boolean;
		onpay: (_provider: string, _amount: number) => Promise<void>;
		acceptInClub?: boolean;
		allowPartialAmount?: boolean;
	} = $props();
	let provider = $state("");
	const form = createAppForm(() => ({
		defaultValues: { amount: amountAvailable.toFixed(2) },
		validators: {
			onChange: ({ value }) => validatePaymentAmount(value.amount, amountAvailable),
			onSubmit: ({ value }) => validatePaymentAmount(value.amount, amountAvailable),
		},
		onSubmit: async ({ value }) => {
			if (!busy && (acceptInClub || provider) && !validatePaymentAmount(value.amount, amountAvailable))
				await onpay(provider, allowPartialAmount ? Number(value.amount) : amountAvailable);
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
	{#if allowPartialAmount}
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
	{/if}
	{#if !acceptInClub && methods.length}
		<p>Choose a payment method</p>
		<ToggleGroup.Root type="single" variant="outline" bind:value={provider} disabled={busy} class="flex flex-wrap">
			{#each methods as method (method.providerName)}
				<ToggleGroup.Item value={method.providerName}>{method.type}</ToggleGroup.Item>
			{/each}
		</ToggleGroup.Root>
	{:else if !acceptInClub}
		<p>No provider payment methods are available. You can still use an eligible voucher.</p>
	{/if}
	<form.Subscribe selector={(state) => ({ valid: state.isValid, submitting: state.isSubmitting })}>
		{#snippet children(state)}
			<Button type="submit" disabled={busy || state.submitting || !state.valid || (!acceptInClub && !provider) || amountAvailable <= 0}
				>{busy ? "Processing..." : acceptInClub ? "Accept payment" : "Pay now"}</Button
			>
		{/snippet}
	</form.Subscribe>
</form>
