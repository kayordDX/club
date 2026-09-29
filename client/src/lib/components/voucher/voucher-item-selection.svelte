<script lang="ts">
	import { Checkbox, Field, Label } from "@kayord/ui";
	import { VoucherRedemptionKind } from "$lib/api/generated/api.schemas";

	let {
		kind,
		isExtra,
		contracts,
		extras,
		contractIds = $bindable([]),
		extraIds = $bindable([]),
	}: {
		kind: number;
		isExtra: boolean;
		contracts: { id: number; name: string }[];
		extras: { id: number; name: string }[];
		contractIds?: number[];
		extraIds?: number[];
	} = $props();

	const items = $derived(isExtra ? extras : contracts);
	const selected = $derived(isExtra ? extraIds : contractIds);
	function toggle(id: number, checked: boolean) {
		const next = checked ? [...new Set([...selected, id])] : selected.filter((value) => value !== id);
		if (isExtra) extraIds = next;
		else contractIds = next;
	}
</script>

{#if kind === VoucherRedemptionKind.Entitlement}
	<Field.Set>
		<Field.Legend>Eligible {isExtra ? "extras" : "contracts"}</Field.Legend>
		<Field.Description>Select every permitted item. No selection means no eligible items.</Field.Description>
		{#each items as item (item.id)}
			<div class="flex items-center gap-2">
				<Checkbox
					id={`voucher-${isExtra ? "extra" : "contract"}-${item.id}`}
					checked={selected.includes(item.id)}
					onCheckedChange={(checked) => toggle(item.id, checked === true)}
				/>
				<Label for={`voucher-${isExtra ? "extra" : "contract"}-${item.id}`}>{item.name}</Label>
			</div>
		{:else}
			<p class="text-muted-foreground text-sm">No {isExtra ? "extras" : "contracts"} configured at this facility.</p>
		{/each}
	</Field.Set>
{:else}
	<p class="text-muted-foreground text-sm">Contract and extra selections do not restrict discounts or credit. Facility restrictions still apply.</p>
{/if}
