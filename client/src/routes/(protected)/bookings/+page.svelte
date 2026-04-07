<script lang="ts">
	import PageHeading from "../settings/PageHeading.svelte";
	import { BookIcon } from "@lucide/svelte";
	import { createBookingGetUser, type BookingDTO } from "$lib/api";
	import {
		type ColumnDef,
		type Updater,
		type PaginationState,
		type SortingState,
		getPaginationRowModel,
		getFilteredRowModel,
		getSortedRowModel,
	} from "@tanstack/table-core";
	import { DataTable, createShadTable, renderSnippet } from "@kayord/ui/data-table";
	import { Actions } from "@kayord/ui";
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";

	const bookingQuery = createBookingGetUser();
	let data = $derived(bookingQuery.data?.items ?? []);
	let rowCount = $derived(bookingQuery.data?.totalCount ?? 0);

	const columns: ColumnDef<BookingDTO>[] = [
		{
			header: "Booking Date",
			accessorKey: "bookingStatusDate",
			size: 1000,
		},
		{
			header: "Status",
			accessorKey: "bookingStatus.name",
			size: 1000,
		},
		{
			header: "Amount Outstanding",
			accessorKey: "amountOutstanding",
			size: 1000,
		},
		{
			header: "",
			accessorKey: "name",
			cell: (item) => renderSnippet(viewBooking, item.row.original),
			size: 10,
			enableSorting: false,
		},
	];

	let pagination: PaginationState = $state({ pageIndex: 0, pageSize: 10 });
	const setPagination = (updater: Updater<PaginationState>) => {
		if (updater instanceof Function) {
			pagination = updater(pagination);
		} else pagination = updater;
	};

	let sorting: SortingState = $state([]);
	const setSorting = (updater: Updater<SortingState>) => {
		if (updater instanceof Function) {
			sorting = updater(sorting);
		} else sorting = updater;
	};

	const table = createShadTable({
		columns,
		get data() {
			return data;
		},
		getFilteredRowModel: getFilteredRowModel(),
		manualPagination: true,
		manualFiltering: true,
		manualSorting: true,
		getSortedRowModel: getSortedRowModel(),
		getPaginationRowModel: getPaginationRowModel(),
		state: {
			get pagination() {
				return pagination;
			},
			get sorting() {
				return sorting;
			},
		},
		get rowCount() {
			return rowCount;
		},
		onPaginationChange: setPagination,
		onSortingChange: setSorting,
		enableRowSelection: false,
	});

	const openBooking = (bookingId: number) => {
		goto(resolve(`/bookings/${bookingId}`));
	};
</script>

{#snippet viewBooking(booking: BookingDTO)}
	<Actions
		actions={[
			{
				icon: BookIcon,
				text: "View Booking",
				class: "truncate",
				action: () => openBooking(booking.id),
			},
		]}
	/>
{/snippet}

<div class="m-4">
	<PageHeading title="Bookings" description="My Bookings" icon={BookIcon} />
	<DataTable
		{table}
		headerClass="pb-2"
		isLoading={bookingQuery.isPending}
		noDataMessage="No bookings"
	/>
</div>
