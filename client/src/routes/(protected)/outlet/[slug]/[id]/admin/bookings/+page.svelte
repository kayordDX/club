<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { adminBookingGetAll } from "$lib/api/remote/admin.remote";
	import type { AdminBookingDTO, PaginatedListOfAdminBookingDTO } from "$lib/api";
	import { BOOKING_STATUS_OPTIONS, statusBadgeVariant, statusLabel } from "$lib/booking/status";
	import { buildBookingFilters, sortingToQueryKitSorts } from "$lib/booking/querykit";
	import { formatCurrency, formatDate, formatTime } from "$lib/booking/format";
	import { type ColumnDef, type PaginationState, type SortingState } from "@tanstack/svelte-table";
	import { DataTable, createShadTable, renderSnippet, type DataTableFeatures } from "@kayord/ui/data-table";
	import { Actions, Badge, Input, Select } from "@kayord/ui";
	import { BookIcon, PencilIcon, SearchIcon } from "@lucide/svelte";

	const facilityId = $derived(Number(page.params.id) || 0);

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

	// Reactive remote fetch — re-runs whenever params change (pagination/sort/filter).
	let result = $state<PaginatedListOfAdminBookingDTO | undefined>();
	let isLoading = $state(true);
	$effect(() => {
		const p = params;
		isLoading = true;
		adminBookingGetAll({ facilityId, params: p })
			.then((r) => {
				result = r;
				isLoading = false;
			})
			.catch(() => {
				isLoading = false;
			});
	});
	let data = $derived(result?.items ?? []);
	let rowCount = $derived(result?.totalCount ?? 0);

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
		{ header: "Customer", accessorKey: "customerName", enableSorting: false },
		{ header: "Players", accessorKey: "playerCount", size: 80 },
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
			id: "actions",
			cell: (item) => renderSnippet(manageCell, item.row.original),
			size: 10,
			enableSorting: false,
		},
	];

	const controlledState = $state({
		pagination: { pageIndex: 0, pageSize: 10 } as PaginationState,
		sorting: [{ id: "slotStartDatetime", desc: true }] as SortingState,
	});

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

	const manage = (bookingId: number) => {
		goto(resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings/${bookingId}`));
	};
</script>

{#snippet statusCell(booking: AdminBookingDTO)}
	<Badge variant={statusBadgeVariant(booking.bookingStatusId)}>{statusLabel(booking.bookingStatusId)}</Badge>
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
	<DataTable {table} headerClass="pb-2" {isLoading} noDataMessage="No bookings">
		{#snippet leftToolbar()}
			<div class="flex items-center gap-2">
				<div class="relative">
					<SearchIcon class="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
					<Input type="search" placeholder="Search booking #" bind:value={searchInput} class="w-48 pl-9" />
				</div>
				<Select.Root
					type="single"
					value={statusValue}
					onValueChange={(v) => {
						statusValue = v ?? "all";
						controlledState.pagination.pageIndex = 0;
					}}
				>
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
