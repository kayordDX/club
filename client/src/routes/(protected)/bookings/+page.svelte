<script lang="ts">
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { BookIcon, PencilIcon, SearchIcon } from "@lucide/svelte";
	import { BookingStatusEnum, createBookingGetUser, type BookingSummaryDTO } from "$lib/api";
	import { type ColumnDef, type Updater, type PaginationState, type SortingState, getPaginationRowModel, getSortedRowModel } from "@tanstack/table-core";
	import { DataTable, createShadTable, renderSnippet } from "@kayord/ui/data-table";
	import { Actions, Badge, Input, Select } from "@kayord/ui";
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { BOOKING_STATUS_OPTIONS, statusBadgeVariant, statusLabel } from "$lib/booking/status";
	import { formatCurrency, formatDate, formatTime } from "$lib/booking/format";
	import { buildBookingFilters, sortingToQueryKitSorts } from "$lib/booking/querykit";

	let pagination: PaginationState = $state({ pageIndex: 0, pageSize: 10 });
	let sorting: SortingState = $state([{ id: "slotStartDatetime", desc: true }]);

	// "all" keeps the single-select value non-empty while meaning "no status filter".
	let statusValue = $state("all");
	let status = $derived(statusValue === "all" ? null : Number(statusValue));

	let searchInput = $state("");
	let search = $state("");
	$effect(() => {
		const value = searchInput;
		const timer = setTimeout(() => {
			search = value;
			pagination.pageIndex = 0;
		}, 350);
		return () => clearTimeout(timer);
	});

	const params = $derived.by(() => {
		const p: Record<string, string | number> = {
			page: pagination.pageIndex + 1,
			pageSize: pagination.pageSize,
		};
		const filters = buildBookingFilters({ status, search });
		if (filters) p.filters = filters;
		const sorts = sortingToQueryKitSorts(sorting);
		if (sorts) p.sorts = sorts;
		return p;
	});

	const bookingQuery = createBookingGetUser(() => params);
	let data = $derived(bookingQuery.data?.items ?? []);
	let rowCount = $derived(bookingQuery.data?.totalCount ?? 0);

	const columns: ColumnDef<BookingSummaryDTO>[] = [
		{ header: "#", accessorKey: "id", size: 60 },
		{
			header: "Facility",
			accessorKey: "facilityName",
			cell: (item) => item.row.original.facilityName ?? "—",
		},
		{
			header: "Date",
			accessorKey: "slotStartDatetime",
			cell: (item) => formatDate(item.row.original.slotStartDatetime),
		},
		{
			header: "Time",
			id: "time",
			cell: (item) => `${formatTime(item.row.original.slotStartDatetime)} – ${formatTime(item.row.original.slotEndDatetime)}`,
			enableSorting: false,
		},
		{
			header: "Status",
			accessorKey: "bookingStatusId",
			cell: (item) => renderSnippet(statusCell, item.row.original),
		},
		{ header: "Players", accessorKey: "playerCount", size: 80 },
		{
			header: "Outstanding",
			accessorKey: "amountOutstanding",
			cell: (item) => formatCurrency(item.row.original.amountOutstanding),
		},
		{
			header: "",
			id: "actions",
			cell: (item) => renderSnippet(viewBooking, item.row.original),
			size: 10,
			enableSorting: false,
		},
	];

	const setPagination = (updater: Updater<PaginationState>) => {
		pagination = updater instanceof Function ? updater(pagination) : updater;
	};

	const setSorting = (updater: Updater<SortingState>) => {
		sorting = updater instanceof Function ? updater(sorting) : updater;
		pagination.pageIndex = 0;
	};

	const table = createShadTable({
		columns,
		get data() {
			return data;
		},
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

	const openEditBooking = (bookingId: number) => {
		goto(resolve(`/bookings/${bookingId}/edit`));
	};
</script>

{#snippet statusCell(booking: BookingSummaryDTO)}
	<Badge variant={statusBadgeVariant(booking.bookingStatusId)}>
		{statusLabel(booking.bookingStatusId)}
	</Badge>
{/snippet}

{#snippet viewBooking(booking: BookingSummaryDTO)}
	<Actions
		actions={[
			{
				icon: BookIcon,
				text: "View Booking",
				class: "truncate",
				action: () => openBooking(booking.id),
			},
			...(booking.bookingStatusId === BookingStatusEnum.Pending
				? [
						{
							icon: PencilIcon,
							text: "Edit Booking",
							class: "truncate",
							action: () => openEditBooking(booking.id),
						},
					]
				: []),
		]}
	/>
{/snippet}

<div class="m-4">
	<PageHeading title="Bookings" description="My Bookings" icon={BookIcon} />
	<DataTable {table} isLoading={bookingQuery.isPending} noDataMessage="No bookings">
		{#snippet leftToolbar()}
			<div class="flex items-center gap-2">
				<div class="relative">
					<SearchIcon class="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
					<Input type="search" placeholder="Search booking #" bind:value={searchInput} class="w-48 pl-9" />
				</div>
				<Select.Root type="single" value={statusValue} onValueChange={(v) => (statusValue = v ?? "all")}>
					<Select.Trigger class="w-40">{status ? statusLabel(status) : "All statuses"}</Select.Trigger>
					<Select.Content>
						<Select.Item value="all" label="All statuses">All statuses</Select.Item>
						{#each BOOKING_STATUS_OPTIONS as option (option.value)}
							<Select.Item value={String(option.value)} label={option.label}>{option.label}</Select.Item>
						{/each}
					</Select.Content>
				</Select.Root>
			</div>
		{/snippet}
	</DataTable>
</div>
