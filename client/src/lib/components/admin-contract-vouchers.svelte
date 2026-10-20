<script lang="ts">
	import { Button, Field, Input, Select } from "@kayord/ui";
	import { PlusIcon, Trash2Icon } from "@lucide/svelte";
	import { z } from "zod";
	import { createAppForm } from "$lib/components/Form";
	import { VoucherRedemptionKind, type AdminContractVoucherDTO, type AdminContractVouchersDTO } from "$lib/api/generated/api.schemas";

	let {
		details,
		onsave,
		oncancel,
	}: {
		details: AdminContractVouchersDTO;
		onsave: (_vouchers: AdminContractVoucherDTO[]) => Promise<void>;
		oncancel: () => void;
	} = $props();
	let saveError = $state("");
	const amountLabel = (id: number) => {
		const kind = details.availableVouchers.find((v) => v.id === id)?.redemptionKind;
		return kind === VoucherRedemptionKind.Credit ? "Credit amount (R)" : kind === VoucherRedemptionKind.Discount ? "Number of uses" : "Number of units";
	};
	const schema = z
		.array(z.object({ voucherId: z.number().int().positive(), amount: z.string() }))
		.max(500)
		.superRefine((links, ctx) => {
			if (new Set(links.map((link) => link.voucherId)).size !== links.length) ctx.addIssue({ code: "custom", message: "Choose each voucher only once." });
			for (const link of links) {
				const voucher = details.availableVouchers.find((v) => v.id === link.voucherId);
				const credit = voucher?.redemptionKind === VoucherRedemptionKind.Credit;
				if (
					!voucher ||
					!(credit ? /^\d+(\.\d{1,2})?$/ : /^\d+$/).test(link.amount) ||
					Number(link.amount) <= 0 ||
					Number(link.amount) > Number.MAX_SAFE_INTEGER
				) {
					ctx.addIssue({ code: "custom", message: "Enter positive whole units/uses, or a credit amount with up to 2 decimal places." });
				}
			}
		});
	const form = createAppForm(() => ({
		defaultValues: { vouchers: details.vouchers.map((link) => ({ voucherId: link.voucherId, amount: String(link.amount) })) },
		validators: { onSubmit: ({ value }) => schema.safeParse(value.vouchers).error?.issues[0]?.message },
		onSubmit: async ({ value }) => {
			saveError = "";
			try {
				await onsave(value.vouchers.map((link) => ({ voucherId: link.voucherId, amount: Number(link.amount) })));
			} catch {
				saveError = "Unable to save contract vouchers. Please try again.";
			}
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
	<p class="text-muted-foreground text-sm">
		Each new member receives these amounts when assigned this contract. Benefits expire on their membership end date. Changes do not alter existing wallet
		balances.
	</p>
	<form.Subscribe selector={(state) => ({ links: state.values.vouchers, pending: state.isSubmitting, errors: state.errors })}>
		{#snippet children(state)}
			{#if details.availableVouchers.length === 0}
				<p class="text-muted-foreground text-sm">No vouchers are available. Create a voucher in this facility's Vouchers section first.</p>
			{:else if state.links.length === 0}
				<p class="text-muted-foreground text-sm">No vouchers linked to this contract.</p>
			{/if}
			{#each state.links as link, index (link.voucherId)}
				<div class="space-y-3 rounded-lg border p-3">
					<Field.Field>
						<Field.Label for={`contract-voucher-${index}`}>Voucher {index + 1}</Field.Label>
						<Select.Root
							type="single"
							value={String(link.voucherId)}
							disabled={state.pending}
							onValueChange={(value) => {
								const selected = details.availableVouchers.find((v) => String(v.id) === value);
								if (selected) form.setFieldValue(`vouchers[${index}].voucherId`, selected.id);
							}}
						>
							<Select.Trigger id={`contract-voucher-${index}`} class="w-full"
								>{details.availableVouchers.find((v) => v.id === link.voucherId)?.name ?? "Select voucher"}</Select.Trigger
							>
							<Select.Content>
								{#each details.availableVouchers.filter((v) => v.id === link.voucherId || !state.links.some((l) => l.voucherId === v.id)) as voucher (voucher.id)}
									<Select.Item value={String(voucher.id)} label={voucher.name}>{voucher.name}</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
						<Field.Description>{details.availableVouchers.find((v) => v.id === link.voucherId)?.description}</Field.Description>
					</Field.Field>
					<div class="flex items-end gap-2">
						<form.AppField name={`vouchers[${index}].amount`}>
							{#snippet children(field)}
								<Field.Field>
									<Field.Label for={`contract-amount-${index}`}>{amountLabel(link.voucherId)}</Field.Label>
									<Input
										id={`contract-amount-${index}`}
										inputmode="decimal"
										value={field.state.value}
										oninput={(event) => field.handleChange(event.currentTarget.value)}
										onblur={() => field.handleBlur()}
										disabled={state.pending}
									/>
								</Field.Field>
							{/snippet}
						</form.AppField>
						<Button
							type="button"
							variant="outline"
							size="icon"
							aria-label={`Remove voucher ${index + 1}`}
							disabled={state.pending}
							onclick={() => form.removeFieldValue("vouchers", index)}><Trash2Icon class="size-4" /></Button
						>
					</div>
				</div>
			{/each}
			<Button
				type="button"
				variant="outline"
				disabled={state.pending || state.links.length >= details.availableVouchers.length}
				onclick={() => {
					const next = details.availableVouchers.find((v) => !state.links.some((l) => l.voucherId === v.id));
					if (next) form.pushFieldValue("vouchers", { voucherId: next.id, amount: "1" });
				}}><PlusIcon class="size-4" />Add voucher</Button
			>
			{#each state.errors as error, index (index)}
				{#if typeof error === "string"}<p role="alert" class="text-destructive text-sm">{error}</p>{/if}
			{/each}
			{#if saveError}<p role="alert" class="text-destructive text-sm">{saveError}</p>{/if}
			<footer class="flex justify-end gap-2">
				<Button type="button" variant="outline" disabled={state.pending} onclick={oncancel}>Cancel</Button>
				<Button type="submit" disabled={state.pending}>{state.pending ? "Saving…" : "Save vouchers"}</Button>
			</footer>
		{/snippet}
	</form.Subscribe>
</form>
