<script lang="ts">
	import { untrack } from "svelte";
	import { z } from "zod";
	import { Button, Checkbox, Field, Label } from "@kayord/ui";
	import { createAppForm, Form } from "$lib/components/Form";
	import { type AdminVoucherDTO, type AdminVoucherCreateRequest, VoucherDiscountMode, VoucherRedemptionKind } from "$lib/api/generated/api.schemas";
	import VoucherItemSelection from "./voucher-item-selection.svelte";

	let {
		voucher,
		contracts,
		extras,
		onSave,
		onCancel,
	}: {
		voucher?: AdminVoucherDTO | null;
		contracts: { id: number; name: string }[];
		extras: { id: number; name: string }[];
		onSave: (_body: AdminVoucherCreateRequest) => Promise<void>;
		onCancel: () => void;
	} = $props();
	const initial = untrack(() => voucher);
	let error = $state("");
	const form = createAppForm(() => ({
		defaultValues: {
			name: initial?.name ?? "",
			description: initial?.description ?? "",
			redemptionKind: initial?.redemptionKind ?? VoucherRedemptionKind.Entitlement,
			isExtra: initial?.isExtra ?? false,
			discountMode: initial?.discountMode ?? VoucherDiscountMode.Percentage,
			discountValue: String(initial?.discountValue ?? ""),
			maxDiscountAmount: String(initial?.maxDiscountAmount ?? ""),
			contractIds: initial?.contractIds ?? ([] as number[]),
			extraIds: initial?.extraIds ?? ([] as number[]),
		},
		validators: {
			onSubmit: z.object({
				name: z.string().trim().min(1).max(200),
				description: z.string(),
				redemptionKind: z.enum(VoucherRedemptionKind),
				isExtra: z.boolean(),
				discountMode: z.enum(VoucherDiscountMode),
				discountValue: z.string(),
				maxDiscountAmount: z.string(),
				contractIds: z.array(z.number().int()),
				extraIds: z.array(z.number().int()),
			}),
		},
		onSubmit: async ({ value }) => {
			error = "";
			const discount = value.redemptionKind === VoucherRedemptionKind.Discount;
			const discountValue = Number(value.discountValue);
			const cap = value.maxDiscountAmount ? Number(value.maxDiscountAmount) : null;
			if (
				discount &&
				(!Number.isFinite(discountValue) ||
					discountValue <= 0 ||
					(value.discountMode === VoucherDiscountMode.Percentage && discountValue > 100) ||
					(cap !== null && (!Number.isFinite(cap) || cap <= 0)))
			) {
				error = "Enter a positive discount value (at most 100 for percentages) and an optional positive cap.";
				return;
			}
			try {
				await onSave({
					name: value.name.trim(),
					description: value.description,
					isExtra: value.isExtra,
					redemptionKind: value.redemptionKind,
					discountMode: discount ? value.discountMode : null,
					discountValue: discount ? discountValue : null,
					maxDiscountAmount: discount ? cap : null,
					contractIds: value.redemptionKind === VoucherRedemptionKind.Entitlement && !value.isExtra ? value.contractIds : [],
					extraIds: value.redemptionKind === VoucherRedemptionKind.Entitlement && value.isExtra ? value.extraIds : [],
				});
			} catch {
				error = "Could not save this voucher. Check your selections and try again.";
			}
		},
	}));
	const values = form.useStore((state) => state.values);
</script>

<Form {form}>
	<div class="flex flex-col gap-4">
		<form.AppField name="name">{#snippet children(field)}<field.Input label="Name" required />{/snippet}</form.AppField>
		<form.AppField name="description">{#snippet children(field)}<field.Input label="Description" />{/snippet}</form.AppField>
		<form.AppField name="redemptionKind">
			{#snippet children(field)}
				<field.Select
					label="Type"
					items={[
						{ value: VoucherRedemptionKind.Entitlement, label: "Entitlement" },
						{ value: VoucherRedemptionKind.Discount, label: "Discount" },
						{ value: VoucherRedemptionKind.Credit, label: "Credit" },
					]}
				/>
			{/snippet}
		</form.AppField>
		<form.Field name="isExtra">
			{#snippet children(field)}
				<div class="flex items-center gap-2">
					<Checkbox id="voucher-is-extra" checked={field.state.value} onCheckedChange={(checked) => field.handleChange(checked === true)} /><Label
						for="voucher-is-extra">Extra-only (otherwise rounds)</Label
					>
				</div>
			{/snippet}
		</form.Field>
		{#if values.current.redemptionKind === VoucherRedemptionKind.Discount}
			<form.AppField name="discountMode">
				{#snippet children(field)}<field.Select
						label="Discount mode"
						items={[
							{ value: VoucherDiscountMode.Percentage, label: "Percentage" },
							{ value: VoucherDiscountMode.FixedAmount, label: "Fixed amount" },
						]}
					/>{/snippet}
			</form.AppField>
			<form.AppField name="discountValue"
				>{#snippet children(field)}<field.Input label="Discount value" type="number" min="0.01" step="0.01" required />{/snippet}</form.AppField
			>
			<form.AppField name="maxDiscountAmount"
				>{#snippet children(field)}<field.Input label="Maximum discount (optional)" type="number" min="0.01" step="0.01" />{/snippet}</form.AppField
			>
		{/if}
		<VoucherItemSelection
			kind={values.current.redemptionKind}
			isExtra={values.current.isExtra}
			{contracts}
			{extras}
			bind:contractIds={() => values.current.contractIds, (ids) => form.setFieldValue("contractIds", ids)}
			bind:extraIds={() => values.current.extraIds, (ids) => form.setFieldValue("extraIds", ids)}
		/>
		{#if error}<p role="alert" class="text-destructive text-sm">{error}</p>{/if}
		<Field.Description>Changes affect future redemptions of existing grants. Grant balances and history are not changed.</Field.Description>
		<div class="flex justify-end gap-2">
			<Button type="button" variant="outline" onclick={onCancel}>Cancel</Button>
			<form.AppForm><form.Submit>{initial ? "Save changes" : "Create voucher"}</form.Submit></form.AppForm>
		</div>
	</div>
</Form>
