<script lang="ts">
	import { page } from "$app/state";
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { adminContractCreate, adminContractDelete, adminContractGetAll, adminContractUpdate } from "$lib/api/remote/admin.remote";
	import type { AdminContractDTO } from "$lib/api";
	import { formatCurrency } from "$lib/booking/format";
	import { type ColumnDef } from "@tanstack/svelte-table";
	import { type CalendarDate, DateFormatter, getLocalTimeZone, parseDate, today } from "@internationalized/date";
	import { DataTable, createShadTable, renderSnippet, type DataTableFeatures } from "@kayord/ui/data-table";
	import { Calendar as DatePickerCalendar } from "@kayord/ui/calendar";
	import { Actions, AlertDialog, Badge, Button, Checkbox, Dialog, Field, Input, Label, Popover } from "@kayord/ui";
	import { CalendarIcon, PencilIcon, PlusIcon, ScrollTextIcon, Trash2Icon, UsersIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";

	const facilityId = $derived(Number(page.params.id) || 0);

	const contracts = $derived(adminContractGetAll(facilityId));
	const data = $derived(contracts.current ?? []);

	// Dialog state — a null editing target means "create".
	let dialogOpen = $state(false);
	let editing = $state<AdminContractDTO | null>(null);
	let isSaving = $state(false);

	// Delete confirmation state.
	let deleteTarget = $state<AdminContractDTO | null>(null);
	let isDeleting = $state(false);

	const manageMembers = (contract: AdminContractDTO) => {
		goto(resolve(`/outlet/${page.params.slug}/${facilityId}/admin/contracts/${contract.id}/members`));
	};

	type FormState = {
		name: string;
		price: number;
		frequency: number;
		startDate: CalendarDate;
		endDate: CalendarDate;
		isActive: boolean;
		isPublic: boolean;
	};

	const dateFormatter = new DateFormatter("en-ZA", { dateStyle: "medium" });
	let startDatePickerOpen = $state(false);
	let endDatePickerOpen = $state(false);
	let formState = $state<FormState>(emptyForm());

	function emptyForm(): FormState {
		const currentDate = today(getLocalTimeZone());
		return { name: "", price: 0, frequency: 12, startDate: currentDate, endDate: currentDate, isActive: true, isPublic: false };
	}

	const toCalendarDate = (iso: string) => parseDate(iso.slice(0, 10));
	const toIso = (date: CalendarDate) => date.toDate(getLocalTimeZone()).toISOString();

	const openCreate = () => {
		editing = null;
		formState = emptyForm();
		dialogOpen = true;
	};

	const openEdit = (contract: AdminContractDTO) => {
		editing = contract;
		formState = {
			name: contract.name,
			price: contract.price,
			frequency: contract.frequency,
			startDate: toCalendarDate(contract.startDate),
			endDate: toCalendarDate(contract.endDate),
			isActive: contract.isActive,
			isPublic: contract.isPublic,
		};
		dialogOpen = true;
	};

	const save = async (e: SubmitEvent) => {
		e.preventDefault();
		if (!formState.name.trim()) {
			toast.error("Name is required.");
			return;
		}
		if (formState.endDate.compare(formState.startDate) < 0) {
			toast.error("End date cannot be before the start date.");
			return;
		}

		isSaving = true;
		const body = {
			name: formState.name.trim(),
			price: Number(formState.price),
			frequency: Number(formState.frequency),
			startDate: toIso(formState.startDate),
			endDate: toIso(formState.endDate),
			isActive: formState.isActive,
			isPublic: formState.isPublic,
		};

		try {
			if (editing) {
				await adminContractUpdate({ facilityId, id: editing.id, body });
				toast.success("Contract updated");
			} else {
				await adminContractCreate({ facilityId, body });
				toast.success("Contract created");
			}
			dialogOpen = false;
			void contracts.refresh();
		} catch (error) {
			console.error("Failed to save contract:", error);
			toast.error("Failed to save contract. Please try again.");
		} finally {
			isSaving = false;
		}
	};

	const confirmDelete = async () => {
		if (!deleteTarget) return;
		isDeleting = true;
		try {
			await adminContractDelete({ facilityId, id: deleteTarget.id });
			toast.success("Contract deleted");
			deleteTarget = null;
			void contracts.refresh();
		} catch (error) {
			console.error("Failed to delete contract:", error);
			toast.error("This contract is in use and cannot be deleted.");
		} finally {
			isDeleting = false;
		}
	};

	const columns: ColumnDef<DataTableFeatures, AdminContractDTO>[] = [
		{ header: "Name", accessorKey: "name" },
		{
			header: "Price",
			accessorKey: "price",
			cell: (item) => formatCurrency(item.row.original.price),
		},
		{
			header: "Frequency",
			accessorKey: "frequency",
			cell: (item) => `${item.row.original.frequency} / year`,
		},
		{
			header: "Status",
			id: "isActive",
			cell: (item) => renderSnippet(activeCell, item.row.original),
		},
		{
			header: "Visibility",
			id: "isPublic",
			cell: (item) => renderSnippet(visibilityCell, item.row.original),
		},
		{
			header: "",
			id: "actions",
			cell: (item) => renderSnippet(actionsCell, item.row.original),
			size: 10,
		},
	];

	const table = createShadTable({
		columns,
		get data() {
			return data;
		},
		enableRowSelection: false,
	});
</script>

{#snippet activeCell(contract: AdminContractDTO)}
	<Badge variant={contract.isActive ? "default" : "secondary"}>{contract.isActive ? "Active" : "Inactive"}</Badge>
{/snippet}

{#snippet visibilityCell(contract: AdminContractDTO)}
	<Badge variant={contract.isPublic ? "default" : "outline"}>{contract.isPublic ? "Public" : "Members only"}</Badge>
{/snippet}

{#snippet actionsCell(contract: AdminContractDTO)}
	<Actions
		actions={[
			{ icon: UsersIcon, text: "Manage members", action: () => manageMembers(contract) },
			{ icon: PencilIcon, text: "Edit", action: () => openEdit(contract) },
			{ icon: Trash2Icon, text: "Delete", action: () => (deleteTarget = contract) },
		]}
	/>
{/snippet}

<div class="m-4">
	<PageHeading title="Contracts" description="Create and manage the membership contracts offered by this facility." icon={ScrollTextIcon} />

	<DataTable {table} headerClass="pb-2" isLoading={contracts.loading} noDataMessage="No contracts yet">
		{#snippet rightToolbar()}
			<Button size="sm" onclick={openCreate}>
				<PlusIcon class="size-4" />
				New contract
			</Button>
		{/snippet}
	</DataTable>
</div>

<Dialog.Root bind:open={dialogOpen}>
	<Dialog.Content class="sm:max-w-lg">
		<Dialog.Header>
			<Dialog.Title>{editing ? "Edit contract" : "New contract"}</Dialog.Title>
			<Dialog.Description>Membership contracts define the price and term members pay for access.</Dialog.Description>
		</Dialog.Header>
		<form onsubmit={save} class="flex flex-col gap-4">
			<Field.Field>
				<Field.Label for="contract-name">Name</Field.Label>
				<Input id="contract-name" bind:value={formState.name} placeholder="e.g. Annual membership" required />
			</Field.Field>

			<div class="grid grid-cols-2 gap-4">
				<Field.Field>
					<Field.Label for="contract-price">Price</Field.Label>
					<Input id="contract-price" type="number" min="0" step="0.01" bind:value={formState.price} />
				</Field.Field>
				<Field.Field>
					<Field.Label for="contract-frequency">Frequency (per year)</Field.Label>
					<Input id="contract-frequency" type="number" min="1" step="1" bind:value={formState.frequency} />
				</Field.Field>
			</div>

			<div class="grid grid-cols-2 gap-4">
				<Field.Field>
					<Field.Label for="contract-start">Start date</Field.Label>
					<Popover.Root bind:open={startDatePickerOpen}>
						<div class="relative">
							<Input id="contract-start" value={dateFormatter.format(formState.startDate.toDate(getLocalTimeZone()))} readonly class="pr-9" />
							<Popover.Trigger>
								{#snippet child({ props })}
									<Button {...props} type="button" variant="ghost" size="icon" class="absolute end-1 top-1/2 size-7 -translate-y-1/2">
										<CalendarIcon class="size-3.5" />
										<span class="sr-only">Select start date</span>
									</Button>
								{/snippet}
							</Popover.Trigger>
						</div>
						<Popover.Content class="w-auto overflow-hidden p-0" align="start">
							<DatePickerCalendar bind:value={formState.startDate} type="single" onValueChange={() => (startDatePickerOpen = false)} captionLayout="dropdown" />
						</Popover.Content>
					</Popover.Root>
				</Field.Field>
				<Field.Field>
					<Field.Label for="contract-end">End date</Field.Label>
					<Popover.Root bind:open={endDatePickerOpen}>
						<div class="relative">
							<Input id="contract-end" value={dateFormatter.format(formState.endDate.toDate(getLocalTimeZone()))} readonly class="pr-9" />
							<Popover.Trigger>
								{#snippet child({ props })}
									<Button {...props} type="button" variant="ghost" size="icon" class="absolute end-1 top-1/2 size-7 -translate-y-1/2">
										<CalendarIcon class="size-3.5" />
										<span class="sr-only">Select end date</span>
									</Button>
								{/snippet}
							</Popover.Trigger>
						</div>
						<Popover.Content class="w-auto overflow-hidden p-0" align="start">
							<DatePickerCalendar bind:value={formState.endDate} type="single" onValueChange={() => (endDatePickerOpen = false)} captionLayout="dropdown" />
						</Popover.Content>
					</Popover.Root>
				</Field.Field>
			</div>

			<div class="flex items-center gap-2">
				<Checkbox id="contract-active" bind:checked={formState.isActive} />
				<Label for="contract-active">Active</Label>
			</div>
			<div class="flex items-center gap-2">
				<Checkbox id="contract-public" bind:checked={formState.isPublic} />
				<Label for="contract-public">Publicly bookable (visible to non-members)</Label>
			</div>

			<Dialog.Footer>
				<Button type="button" variant="outline" onclick={() => (dialogOpen = false)} disabled={isSaving}>Cancel</Button>
				<Button type="submit" disabled={isSaving}>{isSaving ? "Saving..." : editing ? "Save changes" : "Create"}</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>

<AlertDialog.Root open={deleteTarget !== null} onOpenChange={(o) => !o && (deleteTarget = null)}>
	<AlertDialog.Content>
		<AlertDialog.Header>
			<AlertDialog.Title>Delete contract?</AlertDialog.Title>
			<AlertDialog.Description>
				This will permanently delete <span class="font-semibold">{deleteTarget?.name}</span>. Contracts in use by slots or members cannot be deleted.
			</AlertDialog.Description>
		</AlertDialog.Header>
		<AlertDialog.Footer>
			<AlertDialog.Cancel disabled={isDeleting}>Cancel</AlertDialog.Cancel>
			<AlertDialog.Action onclick={confirmDelete} disabled={isDeleting}>{isDeleting ? "Deleting..." : "Delete"}</AlertDialog.Action>
		</AlertDialog.Footer>
	</AlertDialog.Content>
</AlertDialog.Root>
