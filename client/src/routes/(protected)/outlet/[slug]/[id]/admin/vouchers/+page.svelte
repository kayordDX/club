<script lang="ts">
	import { browser } from "$app/environment";
	import { page } from "$app/state";
	import { createMutation, createQuery, useQueryClient } from "@tanstack/svelte-query";
	import type { ColumnDef, PaginationState, SortingState } from "@tanstack/svelte-table";
	import { DataTable, createShadTable, renderSnippet, type DataTableFeatures } from "@kayord/ui/data-table";
	import { Alert, AlertDialog, Badge, Button, Dialog, DropdownMenu, Input } from "@kayord/ui";
	import { EllipsisVerticalIcon, PencilIcon, PlusIcon, SearchIcon, TicketIcon, Trash2Icon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { VoucherDiscountMode, VoucherRedemptionKind, type AdminVoucherCreateRequest, type AdminVoucherDTO } from "$lib/api";
	import { adminVoucherCreate, adminVoucherDelete, adminVoucherGetAll, adminVoucherUpdate } from "$lib/api/remote/admin.remote";
	import AdminVoucherForm from "$lib/components/admin-voucher-form.svelte";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { formatCurrency } from "$lib/booking/format";

	const facilityId = $derived(Number(page.params.id) || 0);
	const queryClient = useQueryClient();
	let openVoucherDialog = $state(false);
	let editing = $state.raw<AdminVoucherDTO | undefined>();
	let deleteTarget = $state.raw<AdminVoucherDTO | undefined>();
	let deleteError = $state("");
	const controlledState = $state({
		pagination: { pageIndex: 0, pageSize: 10 } as PaginationState,
		sorting: [{ id: "name", desc: false }] as SortingState,
		globalFilter: "",
	});
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
		mutationFn: (variables: Parameters<typeof adminVoucherCreate>[0]) => adminVoucherCreate(variables),
		onSuccess: (_voucher, variables) => {
			openVoucherDialog = false;
			controlledState.pagination.pageIndex = 0;
			toast.success("Voucher created");
			void queryClient.invalidateQueries({ queryKey: ["admin-vouchers", variables.facilityId] });
		},
	}));
	const updateVoucher = createMutation(() => ({
		mutationFn: (variables: Parameters<typeof adminVoucherUpdate>[0]) => adminVoucherUpdate(variables),
		onSuccess: (_result, variables) => {
			openVoucherDialog = false;
			controlledState.pagination.pageIndex = 0;
			toast.success("Voucher updated");
			void queryClient.invalidateQueries({ queryKey: ["admin-vouchers", variables.facilityId] });
		},
	}));
	const deleteVoucher = createMutation(() => ({
		mutationFn: (variables: Parameters<typeof adminVoucherDelete>[0]) => adminVoucherDelete(variables),
		onSuccess: (_result, variables) => {
			deleteTarget = undefined;
			controlledState.pagination.pageIndex = 0;
			toast.success("Voucher deleted");
			void queryClient.invalidateQueries({ queryKey: ["admin-vouchers", variables.facilityId] });
		},
	}));
	const isSaving = $derived(createVoucher.isPending || updateVoucher.isPending);
	const openCreate = () => {
		editing = undefined;
		openVoucherDialog = true;
	};
	const openEdit = (voucher: AdminVoucherDTO) => {
		editing = voucher;
		openVoucherDialog = true;
	};
	const openDelete = (voucher: AdminVoucherDTO) => {
		deleteError = "";
		deleteTarget = voucher;
	};
	const saveVoucher = async (body: Omit<AdminVoucherCreateRequest, "facilityId">) => {
		if (editing) await updateVoucher.mutateAsync({ facilityId, id: editing.id, body });
		else await createVoucher.mutateAsync({ facilityId, body });
	};
	const confirmDelete = async () => {
		if (!deleteTarget || deleteVoucher.isPending) return;
		deleteError = "";
		try {
			await deleteVoucher.mutateAsync({ facilityId, id: deleteTarget.id });
		} catch {
			deleteError = "Unable to delete this voucher. Vouchers issued to wallets or linked to contracts cannot be deleted. Refresh the list or try again.";
		}
	};
	const kindLabels = {
		[VoucherRedemptionKind.Entitlement]: "Entitlement",
		[VoucherRedemptionKind.Credit]: "Credit",
		[VoucherRedemptionKind.Discount]: "Discount",
	};
	const data = $derived(vouchers.data ?? []);
	const columns: ColumnDef<DataTableFeatures, AdminVoucherDTO>[] = [
		{ header: "Name", accessorKey: "name" },
		{ header: "Description", accessorKey: "description" },
		{ header: "Type", id: "kind", accessorFn: (voucher) => kindLabels[voucher.redemptionKind] },
		{ header: "Applies to", id: "target", accessorFn: (voucher) => (voucher.isExtra ? "Extras only" : "Rounds only") },
		{
			header: "Discount",
			id: "discount",
			accessorFn: (voucher) =>
				voucher.redemptionKind === VoucherRedemptionKind.Discount
					? voucher.discountMode === VoucherDiscountMode.Percentage
						? `${voucher.discountValue}%`
						: formatCurrency(voucher.discountValue)
					: "—",
		},
		{
			header: "Maximum discount",
			accessorKey: "maxDiscountAmount",
			cell: (item) => (item.row.original.maxDiscountAmount == null ? "—" : formatCurrency(item.row.original.maxDiscountAmount)),
		},
		{
			header: "Usage",
			id: "usage",
			accessorFn: (voucher) => (voucher.isInUse ? "In use" : "Unused"),
			cell: (item) => renderSnippet(usageCell, item.row.original),
		},
		{ header: "", id: "actions", cell: (item) => renderSnippet(actionsCell, item.row.original), size: 10, enableSorting: false, enableGlobalFilter: false },
	];
	const table = createShadTable({
		columns,
		get data() {
			return data;
		},
		controlledState,
		autoResetPageIndex: false,
		enableRowSelection: false,
	});
</script>

{#snippet usageCell(voucher: AdminVoucherDTO)}
	<Badge variant={voucher.isInUse ? "secondary" : "outline"}>{voucher.isInUse ? "In use" : "Unused"}</Badge>
{/snippet}

{#snippet actionsCell(voucher: AdminVoucherDTO)}
	<DropdownMenu.Root>
		<DropdownMenu.Trigger>
			{#snippet child({ props })}
				<Button {...props} variant="ghost" size="icon" aria-label={`Actions for ${voucher.name}`} disabled={isSaving || deleteVoucher.isPending}>
					<EllipsisVerticalIcon class="size-4" />
				</Button>
			{/snippet}
		</DropdownMenu.Trigger>
		<DropdownMenu.Content>
			<DropdownMenu.Item onclick={() => openEdit(voucher)}><PencilIcon class="size-4" />Edit</DropdownMenu.Item>
			<DropdownMenu.Item disabled={voucher.isInUse} onclick={() => openDelete(voucher)}><Trash2Icon class="size-4" />Delete</DropdownMenu.Item>
		</DropdownMenu.Content>
	</DropdownMenu.Root>
{/snippet}

<div class="m-4 space-y-4">
	<PageHeading title="Vouchers" description="Create and manage vouchers that can only be used at this facility." icon={TicketIcon} />
	<p class="text-muted-foreground text-sm">
		Voucher definitions set the benefit. Vouchers in use can be renamed, but their benefits cannot be changed and they cannot be deleted.
	</p>
	{#if vouchers.isPending}<p role="status" class="text-muted-foreground">Loading vouchers...</p>{/if}
	{#if vouchers.isError}
		<Alert.Root variant="destructive">
			<Alert.Title>Unable to load vouchers</Alert.Title>
			<Alert.Description>Please try again.</Alert.Description>
			<Button variant="outline" disabled={vouchers.isFetching} onclick={() => vouchers.refetch()}>Retry</Button>
		</Alert.Root>
	{/if}
	<DataTable
		{table}
		headerClass="pb-2"
		isLoading={vouchers.isPending}
		noDataMessage={vouchers.isError ? "Vouchers could not be loaded" : controlledState.globalFilter ? "No vouchers match your search" : "No vouchers yet"}
	>
		{#snippet leftToolbar()}
			<div class="relative">
				<SearchIcon class="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
				<Input
					type="search"
					aria-label="Search vouchers"
					placeholder="Search vouchers"
					value={controlledState.globalFilter}
					oninput={(event) => table.setGlobalFilter(event.currentTarget.value)}
					class="w-56 pl-9"
				/>
			</div>
		{/snippet}
		{#snippet rightToolbar()}
			<Button size="sm" onclick={openCreate} disabled={isSaving || deleteVoucher.isPending}><PlusIcon class="size-4" />New voucher</Button>
		{/snippet}
	</DataTable>
</div>

<Dialog.Root open={openVoucherDialog} onOpenChange={(open) => !isSaving && (openVoucherDialog = open)}>
	<Dialog.Content class="max-h-[90dvh] overflow-y-auto sm:max-w-lg" showCloseButton={!isSaving}>
		<Dialog.Header>
			<Dialog.Title>{editing ? "Edit voucher" : "New voucher"}</Dialog.Title>
			<Dialog.Description>This voucher will only be usable at the current facility.</Dialog.Description>
		</Dialog.Header>
		{#if openVoucherDialog}
			{#key `${facilityId}:${editing?.id ?? "new"}`}
				<AdminVoucherForm voucher={editing} onsave={saveVoucher} oncancel={() => (openVoucherDialog = false)} />
			{/key}
		{/if}
	</Dialog.Content>
</Dialog.Root>

<AlertDialog.Root open={deleteTarget !== undefined} onOpenChange={(open) => !open && !deleteVoucher.isPending && (deleteTarget = undefined)}>
	<AlertDialog.Content>
		<AlertDialog.Header>
			<AlertDialog.Title>Delete voucher?</AlertDialog.Title>
			<AlertDialog.Description
				>This will permanently delete <span class="font-semibold">{deleteTarget?.name}</span>. Vouchers issued to wallets or linked to contracts cannot be
				deleted.</AlertDialog.Description
			>
		</AlertDialog.Header>
		{#if deleteError}<p role="alert" class="text-destructive text-sm">{deleteError}</p>{/if}
		<AlertDialog.Footer>
			<AlertDialog.Cancel>
				{#snippet child({ props })}
					<Button {...props} variant="outline" disabled={deleteVoucher.isPending}>Cancel</Button>
				{/snippet}
			</AlertDialog.Cancel>
			<Button variant="destructive" onclick={confirmDelete} disabled={deleteVoucher.isPending}>{deleteVoucher.isPending ? "Deleting..." : "Delete"}</Button>
		</AlertDialog.Footer>
	</AlertDialog.Content>
</AlertDialog.Root>
