<script lang="ts">
	import { Button } from "@kayord/ui";
	import { createAppForm } from "$lib/components/Form";
	import { VoucherRedemptionKind, type AdminVoucherDTO, type AdminVoucherIssueRequest } from "$lib/api";

	let { voucher, onsend, oncancel }: { voucher: AdminVoucherDTO; onsend: (_body: AdminVoucherIssueRequest) => Promise<void>; oncancel: () => void } = $props();
	let error = $state("");
	let submitting = $state(false);
	const isCredit = $derived(voucher.redemptionKind === VoucherRedemptionKind.Credit);
	const tomorrow = new Date(Date.now() + 86_400_000);
	const form = createAppForm(() => ({
		defaultValues: { recipient: "", amount: "1", expiryDate: tomorrow.toISOString().slice(0, 10) },
		validators: { onSubmit: ({ value }) => validate(value) },
		onSubmit: async ({ value }) => {
			if (submitting) return;
			error = "";
			submitting = true;
			try {
				await onsend({
					voucherId: voucher.id,
					recipient: value.recipient.trim(),
					amount: Number(value.amount),
					validFrom: new Date().toISOString(),
					expiryDate: new Date(`${value.expiryDate}T23:59:59`).toISOString(),
				});
			} catch (cause) {
				error = getErrorMessage(cause);
			} finally {
				submitting = false;
			}
		},
	}));
	function validate(value: { recipient: string; amount: string; expiryDate: string }) {
		if (!value.recipient.trim()) return "Enter an email address or phone number.";
		const amountPattern = isCredit ? /^\d+(\.\d{1,2})?$/ : /^\d+$/;
		if (!amountPattern.test(value.amount) || !Number.isFinite(Number(value.amount)) || Number(value.amount) <= 0) {
			return isCredit ? "Enter a valid positive amount (up to 2 decimal places)." : "Enter a valid positive whole quantity.";
		}
		const expiry = new Date(`${value.expiryDate}T23:59:59`);
		if (!value.expiryDate || Number.isNaN(expiry.getTime()) || expiry <= new Date()) return "Expiry date must be in the future.";
	}
	function getErrorMessage(cause: unknown): string {
		if (cause instanceof Error && cause.message) return cause.message;
		if (cause && typeof cause === "object") {
			const value = cause as { body?: { message?: unknown }; message?: unknown };
			if (typeof value.body?.message === "string") return value.body.message;
			if (typeof value.message === "string") return value.message;
		}
		return "Unable to send voucher.";
	}
</script>

<p class="text-muted-foreground text-sm">
	Enter the recipient’s exact email address or phone number as stored. A ZAR wallet will be created automatically if needed.
</p>
<form
	class="space-y-4"
	onsubmit={(event) => {
		event.preventDefault();
		if (!submitting) form.handleSubmit();
	}}
>
	<form.AppField name="recipient">
		{#snippet children(field)}<field.Input label="Recipient email or phone" autocomplete="off" disabled={submitting} />{/snippet}
	</form.AppField>
	<form.AppField name="amount">
		{#snippet children(field)}<field.Input
				label={isCredit ? "Amount (ZAR)" : "Quantity"}
				inputmode={isCredit ? "decimal" : "numeric"}
				disabled={submitting}
			/>{/snippet}
	</form.AppField>
	<form.AppField name="expiryDate">
		{#snippet children(field)}<field.Input label="Expiry date" type="date" disabled={submitting} />{/snippet}
	</form.AppField>
	{#if error}<p role="alert" class="text-destructive text-sm">{error}</p>{/if}
	<form.Subscribe selector={(state) => state.errors}>
		{#snippet children(errors)}
			{#each errors.flat(Infinity) as validationError, index (index)}
				{#if typeof validationError === "string"}<p role="alert" class="text-destructive text-sm">{validationError}</p>{/if}
			{/each}
		{/snippet}
	</form.Subscribe>
	<form.Subscribe selector={(state) => state.isSubmitting || submitting}>
		{#snippet children(pending)}
			<footer class="flex justify-end gap-2">
				<Button type="button" variant="outline" disabled={pending} onclick={oncancel}>Cancel</Button>
				<Button type="submit" disabled={pending}>{pending ? "Sending…" : "Send voucher"}</Button>
			</footer>
		{/snippet}
	</form.Subscribe>
</form>
