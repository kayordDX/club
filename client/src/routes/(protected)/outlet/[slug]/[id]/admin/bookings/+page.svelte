<script lang="ts">
	import { browser } from "$app/environment";
	import { createQuery } from "@tanstack/svelte-query";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { adminBookingGetAll } from "$lib/api/remote/admin.remote";
	import type { AdminBookingDTO } from "$lib/api";
	import { BOOKING_STATUS_OPTIONS, statusBadgeVariant, statusLabel } from "$lib/booking/status";
	import { buildBookingFilters, sortingToQueryKitSorts } from "$lib/booking/querykit";
	import { formatCurrency, formatDate, formatTime } from "$lib/booking/format";
	import { type ColumnDef, type PaginationState, type SortingState } from "@tanstack/svelte-table";
	import { DataTable, createShadTable, renderSnippet, type DataTableFeatures } from "@kayord/ui/data-table";
	import { Alert, Badge, Button, Card, Input, Select } from "@kayord/ui";
	import { BookIcon, SearchIcon } from "@lucide/svelte";

	const facilityId = $derived(Number(page.params.id) || 0);

	const controlledState = $state({
		pagination: { pageIndex: 0, pageSize: 10 } as PaginationState,
		sorting: [{ id: "slotStartDatetime", desc: true }] as SortingState,
	});

	let statusValue = $state("all");
	let status = $derived(statusValue === "all" ? null : Number(statusValue));

	let searchInput = $state("");
	let search = $state("");
	$effect(() => {
		const value = searchInput;
		const timer = setTimeout(() => {
			search = value;
			controlledState.pagination.pageIndex = 0;
		}, 350);
		return () => clearTimeout(timer);
	});

	const params = $derived.by(() => {
		const p: Record<string, string | number> = {
			page: controlledState.pagination.pageIndex + 1,
			pageSize: controlledState.pagination.pageSize,
		};
		const filters = buildBookingFilters({ status, search });
		if (filters) p.filters = filters;
		const sorts = sortingToQueryKitSorts(controlledState.sorting);
		if (sorts) p.sorts = sorts;
		return p;
	});

	const bookings = createQuery(() => ({
		queryKey: ["admin-bookings", facilityId, params],
		enabled: browser && facilityId > 0,
		queryFn: async () => {
			const request = adminBookingGetAll({ facilityId, params });
			await request.refresh();
			return await request;
		},
	}));
	const data = $derived(bookings.data?.items ?? []);
	const rowCount = $derived(bookings.data?.totalCount ?? 0);
	const isLoading = $derived(bookings.isFetching);

	const columns: ColumnDef<DataTableFeatures, AdminBookingDTO>[] = [
		{ header: "#", accessorKey: "id", size: 60 },
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
		{ header: "Booked by", accessorKey: "customerName", cell: (item) => item.row.original.customerName ?? "—", enableSorting: false },
		{ header: "Players", accessorKey: "playerCount", size: 80 },
		{
			header: "Outstanding",
			accessorKey: "amountOutstanding",
			cell: (item) => formatCurrency(item.row.original.amountOutstanding),
		},
		{
			header: "",
			id: "actions",
			cell: (item) => renderSnippet(manageCell, item.row.original),
			size: 10,
			enableSorting: false,
		},
	];

	const table = createShadTable({
		columns,
		controlledState,
		get data() {
			return data;
		},
		manualPagination: true,
		manualFiltering: true,
		manualSorting: true,
		get rowCount() {
			return rowCount;
		},
		enableRowSelection: false,
	});
</script>

{#snippet statusCell(booking: AdminBookingDTO)}
	<Badge variant={statusBadgeVariant(booking.bookingStatusId)}>{statusLabel(booking.bookingStatusId)}</Badge>
{/snippet}

{#snippet manageCell(booking: AdminBookingDTO)}
	<Button
		href={resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings/${booking.id}`)}
		variant="outline"
		size="sm"
		aria-label={`Manage booking #${booking.id}`}
	>
		Manage
	</Button>
{/snippet}

<div class="m-4 space-y-6">
	<PageHeading title="Bookings" description="View and manage bookings for this facility." icon={BookIcon} />
	{#if bookings.isError}
		<Alert.Root variant="destructive">
			<Alert.Title>Unable to load bookings</Alert.Title>
			<Alert.Description>{bookings.error.message}</Alert.Description>
		</Alert.Root>
		<Button variant="outline" onclick={() => bookings.refetch()}>Retry</Button>
	{/if}
	<Card.Root class="min-w-0 p-4">
		<DataTable {table} headerClass="pb-2" {isLoading} noDataMessage={bookings.isError ? "Bookings could not be loaded" : "No bookings match your filters"}>
			{#snippet leftToolbar()}
				<div class="flex flex-wrap items-center gap-2">
					<div class="relative">
						<SearchIcon class="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
						<Input type="search" aria-label="Search booking number" placeholder="Search booking #" bind:value={searchInput} class="w-48 pl-9" />
					</div>
					<Select.Root
						type="single"
						value={statusValue}
						onValueChange={(v) => {
							statusValue = v ?? "all";
							controlledState.pagination.pageIndex = 0;
						}}
					>
						<Select.Trigger class="w-40" aria-label="Filter by status">{status ? statusLabel(status) : "All statuses"}</Select.Trigger>
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
	</Card.Root>
</div>
