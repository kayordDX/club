<script lang="ts">
	import { Button, Checkbox, Label, Select } from "@kayord/ui";
	import { createAppForm } from "$lib/components/Form";
	import type { AdminVoucherCreateRequest } from "$lib/api/generated/api.schemas";
	import { VoucherDiscountMode, VoucherRedemptionKind } from "$lib/api/generated/api.schemas";

	let { onsave, oncancel }: { onsave: (_body: Omit<AdminVoucherCreateRequest, "facilityId">) => Promise<void>; oncancel: () => void } = $props();
	let saveError = $state("");
	let pending = $state(false);
	const redemptionKinds = [
		{ value: VoucherRedemptionKind.Entitlement, label: "Entitlement" },
		{ value: VoucherRedemptionKind.Credit, label: "Credit" },
		{ value: VoucherRedemptionKind.Discount, label: "Discount" },
	];
	const discountModes = [
		{ value: VoucherDiscountMode.Percentage, label: "Percentage" },
		{ value: VoucherDiscountMode.FixedAmount, label: "Fixed amount" },
	];
	const form = createAppForm(() => ({
		defaultValues: {
			name: "",
			description: "",
			isExtra: false,
			redemptionKind: VoucherRedemptionKind.Entitlement as VoucherRedemptionKind,
			discountMode: VoucherDiscountMode.Percentage as VoucherDiscountMode,
			discountValue: "",
			maxDiscountAmount: "",
		} as {
			name: string;
			description: string;
			isExtra: boolean;
			redemptionKind: VoucherRedemptionKind;
			discountMode: VoucherDiscountMode;
			discountValue: string;
			maxDiscountAmount: string;
		},
		validators: {
			onSubmit: ({ value }) => validate(value),
		},
		onSubmit: async ({ value }) => {
			saveError = "";
			pending = true;
			const body = {
				name: value.name.trim(),
				description: value.description.trim(),
				isExtra: value.isExtra,
				redemptionKind: value.redemptionKind,
				discountMode: value.redemptionKind === VoucherRedemptionKind.Discount ? value.discountMode : null,
				discountValue: value.redemptionKind === VoucherRedemptionKind.Discount ? Number(value.discountValue) : null,
				maxDiscountAmount: value.redemptionKind === VoucherRedemptionKind.Discount && value.maxDiscountAmount !== "" ? Number(value.maxDiscountAmount) : null,
			} satisfies Omit<AdminVoucherCreateRequest, "facilityId">;
			try {
				await onsave(body);
			} catch {
				saveError = "Unable to save voucher. Please try again.";
			} finally {
				pending = false;
			}
		},
	}));

	function validate(value: {
		name: string;
		description: string;
		redemptionKind: number;
		discountValue: string;
		maxDiscountAmount: string;
		discountMode: VoucherDiscountMode;
	}) {
		if (!value.name.trim()) return "Name is required.";
		if (value.name.length > 250) return "Name must be 250 characters or fewer.";
		if (value.description.length > 2000) return "Description must be 2000 characters or fewer.";
		if (value.redemptionKind !== VoucherRedemptionKind.Discount) return undefined;
		if (value.discountMode !== VoucherDiscountMode.Percentage && value.discountMode !== VoucherDiscountMode.FixedAmount) return "Choose a discount mode.";
		if (!/^\d+(\.\d{1,2})?$/.test(value.discountValue) || Number(value.discountValue) <= 0) return "Enter a positive amount with up to 2 decimal places.";
		if (value.discountMode === VoucherDiscountMode.Percentage && Number(value.discountValue) > 100) return "Percentage cannot exceed 100.";
		if (value.maxDiscountAmount !== "" && (!/^\d+(\.\d{1,2})?$/.test(value.maxDiscountAmount) || Number(value.maxDiscountAmount) <= 0)) {
			return "Maximum discount must be a positive amount with up to 2 decimal places.";
		}
		return undefined;
	}
</script>

<p class="text-muted-foreground mb-4 text-sm">Voucher definitions are created here. Issuance balances and expiry are assigned separately.</p>
<form
	class="space-y-4"
	onsubmit={(event) => {
		event.preventDefault();
		form.handleSubmit();
	}}
>
	<form.AppField name="name">
		{#snippet children(field)}<field.Input label="Name" maxlength={250} disabled={pending} />{/snippet}
	</form.AppField>
	<form.AppField name="description">
		{#snippet children(field)}<field.Input label="Description" maxlength={2000} disabled={pending} />{/snippet}
	</form.AppField>
	<form.AppField name="isExtra">
		{#snippet children(field)}
			<div class="flex items-center gap-2">
				<Checkbox id="use-extras" disabled={pending} checked={field.state.value} onCheckedChange={(checked) => field.handleChange(checked === true)} />
				<Label for="use-extras">Use for extras instead of rounds</Label>
			</div>
		{/snippet}
	</form.AppField>
	<form.AppField name="redemptionKind">
		{#snippet children(field)}
			<Label for="redemptionKind">Redemption kind</Label>
			<Select.Root
				type="single"
				name={field.name}
				disabled={pending}
				value={String(field.state.value)}
				onOpenChange={() => field.handleBlur()}
				onValueChange={(value) => {
					const selected = redemptionKinds.find((option) => String(option.value) === value);
					if (selected) field.handleChange(selected.value);
				}}
			>
				<Select.Trigger id="redemptionKind" class="mt-1 w-full">
					{redemptionKinds.find((option) => option.value === field.state.value)?.label ?? "Select a voucher type"}
				</Select.Trigger>
				<Select.Content>
					{#each redemptionKinds as option (option.value)}
						<Select.Item value={String(option.value)} label={option.label}>{option.label}</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
		{/snippet}
	</form.AppField>
	<form.Subscribe selector={(state) => ({ kind: state.values.redemptionKind, pending: state.isSubmitting })}>
		{#snippet children(state)}
			{#if state.kind === VoucherRedemptionKind.Discount}
				<form.AppField name="discountMode">
					{#snippet children(field)}
						<Label for="discountMode">Discount mode</Label>
						<Select.Root
							type="single"
							name={field.name}
							disabled={pending}
							value={String(field.state.value)}
							onOpenChange={() => field.handleBlur()}
							onValueChange={(value) => {
								const selected = discountModes.find((option) => String(option.value) === value);
								if (selected) field.handleChange(selected.value);
							}}
						>
							<Select.Trigger id="discountMode" class="mt-1 w-full">
								{discountModes.find((option) => option.value === field.state.value)?.label ?? "Select a discount mode"}
							</Select.Trigger>
							<Select.Content>
								{#each discountModes as option (option.value)}
									<Select.Item value={String(option.value)} label={option.label}>{option.label}</Select.Item>
								{/each}
							</Select.Content>
						</Select.Root>
					{/snippet}
				</form.AppField>
				<form.AppField name="discountValue"
					>{#snippet children(field)}<field.Input label="Discount value" inputmode="decimal" disabled={pending} />{/snippet}</form.AppField
				>
				<form.AppField name="maxDiscountAmount"
					>{#snippet children(field)}<field.Input label="Maximum discount amount (optional)" inputmode="decimal" disabled={pending} />{/snippet}</form.AppField
				>
			{/if}
		{/snippet}
	</form.Subscribe>
	{#if saveError}<p role="alert" class="text-destructive text-sm">{saveError}</p>{/if}
	<form.Subscribe selector={(state) => state.errors}>
		{#snippet children(errors)}
			{#each errors.flat(Infinity) as error, index (index)}
				{#if typeof error === "string"}<p role="alert" class="text-destructive text-sm">{error}</p>{/if}
			{/each}
		{/snippet}
	</form.Subscribe>
	<footer class="flex justify-end gap-2">
		<form.Subscribe selector={(state) => state.isSubmitting}>
			{#snippet children(pending)}
				<Button type="button" variant="outline" disabled={pending} onclick={oncancel}>Cancel</Button>
				<Button type="submit" disabled={pending}>{pending ? "Saving…" : "Save voucher"}</Button>
			{/snippet}
		</form.Subscribe>
	</footer>
</form>
