<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { goto } from "$app/navigation";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { createAdminBookingGetAll, type AdminBookingDTO } from "$lib/api";
	import { statusBadgeVariant } from "$lib/admin/booking";
	import { formatCurrency, formatDate, formatTime } from "$lib/booking/format";
	import { type ColumnDef, type PaginationState, type Updater, getPaginationRowModel, getFilteredRowModel, getSortedRowModel } from "@tanstack/table-core";
	import { DataTable, createShadTable, renderSnippet } from "@kayord/ui/data-table";
	import { Actions, Badge } from "@kayord/ui";
	import { BookIcon, PencilIcon } from "@lucide/svelte";

	const facilityId = $derived(Number(page.params.id) || 0);

	let pagination: PaginationState = $state({ pageIndex: 0, pageSize: 10 });
	const setPagination = (updater: Updater<PaginationState>) => {
		pagination = updater instanceof Function ? updater(pagination) : updater;
	};

	const params = $derived({
		page: pagination.pageIndex + 1,
		pageSize: pagination.pageSize,
	});

	const bookingQuery = createAdminBookingGetAll(
		() => facilityId,
		() => params
	);
	let data = $derived(bookingQuery.data?.items ?? []);
	let rowCount = $derived(bookingQuery.data?.totalCount ?? 0);

	const columns: ColumnDef<AdminBookingDTO>[] = [
		{ header: "#", accessorKey: "id", size: 60 },
		{
			header: "Date",
			accessorKey: "slotStartDatetime",
			cell: (item) => formatDate(item.row.original.slotStartDatetime),
		},
		{
			header: "Time",
			accessorKey: "slotStartDatetime",
			cell: (item) => formatTime(item.row.original.slotStartDatetime),
			enableSorting: false,
		},
		{
			header: "Status",
			accessorKey: "bookingStatusName",
			cell: (item) => renderSnippet(statusCell, item.row.original),
			enableSorting: false,
		},
		{ header: "Customer", accessorKey: "customerName", enableSorting: false },
		{ header: "Players", accessorKey: "playerCount", size: 80, enableSorting: false },
		{ header: "Extras", accessorKey: "extraCount", size: 80, enableSorting: false },
		{
			header: "Outstanding",
			accessorKey: "amountOutstanding",
			cell: (item) => formatCurrency(item.row.original.amountOutstanding),
		},
		{
			header: "Paid",
			accessorKey: "isPaid",
			cell: (item) => (item.row.original.isPaid ? "Yes" : "No"),
			enableSorting: false,
			size: 60,
		},
		{
			header: "",
			accessorKey: "id",
			cell: (item) => renderSnippet(manageCell, item.row.original),
			size: 10,
			enableSorting: false,
		},
	];

	const table = createShadTable({
		columns,
		get data() {
			return data;
		},
		getFilteredRowModel: getFilteredRowModel(),
		manualPagination: true,
		manualFiltering: true,
		manualSorting: false,
		getSortedRowModel: getSortedRowModel(),
		getPaginationRowModel: getPaginationRowModel(),
		state: {
			get pagination() {
				return pagination;
			},
		},
		get rowCount() {
			return rowCount;
		},
		onPaginationChange: setPagination,
		enableRowSelection: false,
	});

	const manage = (bookingId: number) => {
		goto(resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/${bookingId}`));
	};
</script>

{#snippet statusCell(booking: AdminBookingDTO)}
	<Badge variant={statusBadgeVariant(booking.bookingStatusId)}>{booking.bookingStatusName}</Badge>
{/snippet}

{#snippet manageCell(booking: AdminBookingDTO)}
	<Actions
		actions={[
			{
				icon: PencilIcon,
				text: "Manage",
				class: "truncate",
				action: () => manage(booking.id),
			},
		]}
	/>
{/snippet}

<div class="m-4">
	<PageHeading title="Bookings" description="Manage every booking for this facility — change status and edit details." icon={BookIcon} />
	<DataTable {table} headerClass="pb-2" isLoading={bookingQuery.isPending} noDataMessage="No bookings" />
</div>
