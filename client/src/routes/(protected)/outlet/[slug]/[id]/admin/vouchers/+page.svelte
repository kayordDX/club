<script lang="ts">
	import { page } from "$app/state";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import VoucherEditor from "$lib/components/voucher/voucher-editor.svelte";
	import { adminContractGetAll, adminVoucherCreate, adminVoucherGetAll, adminVoucherUpdate } from "$lib/api/remote/admin.remote";
	import { extraGetFacility } from "$lib/api/remote/extra.remote";
	import { type AdminVoucherDTO, type AdminVoucherCreateRequest, VoucherRedemptionKind } from "$lib/api/generated/api.schemas";
	import { Button, Dialog } from "@kayord/ui";
	import { PlusIcon, TicketIcon } from "@lucide/svelte";
	import { DataTable, createShadTable, renderSnippet, type DataTableFeatures } from "@kayord/ui/data-table";
	import type { ColumnDef } from "@tanstack/svelte-table";
	import { toast } from "svelte-sonner";

	const facilityId = $derived(Number(page.params.id) || 0);
	const vouchers = $derived(adminVoucherGetAll(facilityId));
	const contracts = $derived(adminContractGetAll(facilityId));
	const extras = $derived(extraGetFacility(facilityId));
	let open = $state(false);
	let editing = $state<AdminVoucherDTO | null>(null);
	function edit(voucher: AdminVoucherDTO | null) {
		editing = voucher;
		open = true;
	}
	async function save(body: AdminVoucherCreateRequest) {
		if (editing) await adminVoucherUpdate({ facilityId, id: editing.id, body });
		else await adminVoucherCreate({ facilityId, body });
		await vouchers.refresh();
		open = false;
		toast.success("Voucher saved");
	}
	const columns: ColumnDef<DataTableFeatures, AdminVoucherDTO>[] = [
		{ header: "Name", accessorKey: "name" },
		{ header: "Type", cell: ({ row }) => Object.entries(VoucherRedemptionKind).find(([, value]) => value === row.original.redemptionKind)?.[0] ?? "" },
		{ header: "Items", cell: ({ row }) => (row.original.isExtra ? "Extras" : "Rounds") },
		{
			header: "Eligibility",
			cell: ({ row }) =>
				row.original.redemptionKind === VoucherRedemptionKind.Entitlement
					? `${(row.original.isExtra ? row.original.extraIds : row.original.contractIds).length} selected`
					: "Facility only",
		},
		{ header: "", id: "actions", cell: ({ row }) => renderSnippet(actions, row.original) },
	];
	const table = createShadTable({
		columns,
		get data() {
			return vouchers.current ?? [];
		},
		enableRowSelection: false,
	});
</script>

{#snippet actions(voucher: AdminVoucherDTO)}
	<Button variant="outline" size="sm" disabled={!voucher.canEdit} onclick={() => edit(voucher)}
		>{voucher.canEdit ? "Edit" : "Shared voucher (read only)"}</Button
	>
{/snippet}

<div class="m-4">
	<PageHeading title="Vouchers" description="Manage vouchers at this facility and select the contracts or extras entitlements can redeem." icon={TicketIcon} />
	{#if vouchers.error}<p role="alert" class="text-destructive">Could not load vouchers.</p>{/if}
	<DataTable {table} isLoading={vouchers.loading} noDataMessage="No vouchers at this facility">
		{#snippet rightToolbar()}<Button size="sm" onclick={() => edit(null)}><PlusIcon class="size-4" />New voucher</Button>{/snippet}
	</DataTable>
</div>

<Dialog.Root bind:open>
	<Dialog.Content class="max-h-[90vh] overflow-y-auto sm:max-w-lg">
		<Dialog.Header
			><Dialog.Title>{editing ? "Edit voucher" : "New voucher"}</Dialog.Title><Dialog.Description
				>Empty entitlement selections permit no items. Discounts and credits ignore item lists.</Dialog.Description
			></Dialog.Header
		>
		{#if contracts.error || extras.error}
			<p role="alert">Could not load eligible items. Close and try again.</p>
		{:else if contracts.loading || extras.loading}
			<p>Loading items...</p>
		{:else if open}
			{#key editing?.id ?? "new"}<VoucherEditor
					voucher={editing}
					contracts={contracts.current ?? []}
					extras={extras.current ?? []}
					onSave={save}
					onCancel={() => (open = false)}
				/>{/key}
		{/if}
	</Dialog.Content>
</Dialog.Root>
