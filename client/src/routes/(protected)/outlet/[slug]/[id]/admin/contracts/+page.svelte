<script lang="ts">
	import { page } from "$app/state";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { adminContractCreate, adminContractDelete, adminContractGetAll, adminContractUpdate } from "$lib/api/remote/admin.remote";
	import type { AdminContractDTO } from "$lib/api";
	import { formatCurrency } from "$lib/booking/format";
	import { type ColumnDef } from "@tanstack/svelte-table";
	import { DataTable, createShadTable, renderSnippet, type DataTableFeatures } from "@kayord/ui/data-table";
	import { Actions, AlertDialog, Badge, Button, Checkbox, Dialog, Field, Input, Label } from "@kayord/ui";
	import { PencilIcon, PlusIcon, ScrollTextIcon, Trash2Icon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";

	const facilityId = $derived(Number(page.params.id) || 0);

	let result = $state<AdminContractDTO[] | undefined>();
	let isLoading = $state(true);

	const load = () => {
		isLoading = true;
		adminContractGetAll({ facilityId })
			.then((r) => {
				result = r;
				isLoading = false;
			})
			.catch(() => {
				isLoading = false;
				toast.error("Failed to load contracts.");
			});
	};

	$effect(() => {
		if (facilityId > 0) load();
	});

	let data = $derived(result ?? []);

	// Dialog state — a null editing target means "create".
	let dialogOpen = $state(false);
	let editing = $state<AdminContractDTO | null>(null);
	let isSaving = $state(false);

	// Delete confirmation state.
	let deleteTarget = $state<AdminContractDTO | null>(null);
	let isDeleting = $state(false);

	type FormState = {
		name: string;
		price: number;
		frequency: number;
		startDate: string;
		endDate: string;
		isActive: boolean;
		isPublic: boolean;
	};

	let formState = $state<FormState>(emptyForm());

	function emptyForm(): FormState {
		const today = new Date().toISOString().slice(0, 10);
		return { name: "", price: 0, frequency: 12, startDate: today, endDate: today, isActive: true, isPublic: false };
	}

	// HTML date inputs use yyyy-MM-dd; the API exposes full ISO datetimes.
	const toDateInput = (iso: string) => (iso ? iso.slice(0, 10) : "");
	const toIso = (date: string) => new Date(`${date}T00:00:00.000Z`).toISOString();

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
			startDate: toDateInput(contract.startDate),
			endDate: toDateInput(contract.endDate),
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
		if (formState.endDate < formState.startDate) {
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
			load();
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
			load();
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
			{ icon: PencilIcon, text: "Edit", action: () => openEdit(contract) },
			{ icon: Trash2Icon, text: "Delete", action: () => (deleteTarget = contract) },
		]}
	/>
{/snippet}

<div class="m-4">
	<PageHeading title="Contracts" description="Create and manage the membership contracts offered by this facility." icon={ScrollTextIcon} />

	<DataTable {table} headerClass="pb-2" {isLoading} noDataMessage="No contracts yet">
		{#snippet leftToolbar()}
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
					<Input id="contract-start" type="date" bind:value={formState.startDate} />
				</Field.Field>
				<Field.Field>
					<Field.Label for="contract-end">End date</Field.Label>
					<Input id="contract-end" type="date" bind:value={formState.endDate} />
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
