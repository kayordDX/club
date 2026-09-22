<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import {
		adminContractAddMember,
		adminContractCreateMember,
		adminContractGetAll,
		adminContractGetMembers,
		adminContractSearchMember,
		adminContractUpdateMember,
	} from "$lib/api/remote/admin.remote";
	import { adminContractRemoveMember } from "$lib/api/remote/admin.remote";
	import type { AdminContractMemberDTO, AdminMemberSearchResultDTO } from "$lib/api";
	import { formatDate } from "$lib/booking/format";
	import { type ColumnDef } from "@tanstack/svelte-table";
	import { type CalendarDate, DateFormatter, getLocalTimeZone, parseDate, today } from "@internationalized/date";
	import { DataTable, createShadTable, renderSnippet, type DataTableFeatures } from "@kayord/ui/data-table";
	import { Calendar as DatePickerCalendar } from "@kayord/ui/calendar";
	import { Actions, AlertDialog, Badge, Button, Dialog, Field, Input, Popover } from "@kayord/ui";
	import { CalendarIcon, ChevronLeftIcon, PencilIcon, PlusIcon, SearchIcon, Trash2Icon, UsersIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";

	const facilityId = $derived(Number(page.params.id) || 0);
	const contractId = $derived(Number(page.params.contractId) || 0);
	const slug = $derived(page.params.slug ?? "");

	const contractsBackHref = $derived(resolve(`/outlet/${slug}/${facilityId}/admin/contracts`));

	// Reuse the contracts query to show which contract these members belong to.
	const contracts = $derived(adminContractGetAll(facilityId));
	const contract = $derived((contracts.current ?? []).find((c) => c.id === contractId));

	const membersQuery = $derived(adminContractGetMembers({ facilityId, id: contractId }));
	const members = $derived(membersQuery.current ?? []);

	// Add-member dialog state.
	let addOpen = $state(false);
	let searchInput = $state("");
	let isSearching = $state(false);
	let hasSearched = $state(false);
	let searchResult = $state<AdminMemberSearchResultDTO | null>(null);
	let isAdding = $state(false);
	let addEndDate = $state<CalendarDate>(today(getLocalTimeZone()));
	let addEndDatePickerOpen = $state(false);

	// New-profile form (shown when the search finds nothing).
	let showCreate = $state(false);
	let createFirstName = $state("");
	let createLastName = $state("");
	let createEmail = $state("");
	let createPhone = $state("");
	let isCreating = $state(false);

	// Edit membership state.
	let editTarget = $state<AdminContractMemberDTO | null>(null);
	let editEndDate = $state<CalendarDate>(today(getLocalTimeZone()));
	let editEndDatePickerOpen = $state(false);
	let isUpdating = $state(false);

	// Remove confirmation state.
	let removeTarget = $state<AdminContractMemberDTO | null>(null);
	let isRemoving = $state(false);

	const dateFormatter = new DateFormatter("en-ZA", { dateStyle: "medium" });
	const getDefaultEndDate = () => today(getLocalTimeZone()).add({ months: contract?.frequency ?? 12 });
	const toCalendarDate = (iso: string) => parseDate(iso.slice(0, 10));
	const toApiDate = (date: CalendarDate) => `${date.toString()}T00:00:00.000Z`;

	const resetDialog = () => {
		searchInput = "";
		hasSearched = false;
		searchResult = null;
		showCreate = false;
		createFirstName = "";
		createLastName = "";
		createEmail = "";
		createPhone = "";
		addEndDate = getDefaultEndDate();
	};

	const openAdd = () => {
		resetDialog();
		addOpen = true;
	};

	const runSearch = async (e: SubmitEvent) => {
		e.preventDefault();
		const query = searchInput.trim();
		if (!query) {
			toast.error("Enter an email address or cellphone number.");
			return;
		}

		isSearching = true;
		hasSearched = false;
		searchResult = null;
		showCreate = false;
		try {
			const result = await adminContractSearchMember({ facilityId, id: contractId, params: { query } });
			searchResult = result ?? null;
			hasSearched = true;
			if (!searchResult) {
				// Not found — prime the create form with the search term when it looks like an email.
				createEmail = query.includes("@") ? query : "";
				createPhone = query.includes("@") ? "" : query;
			}
		} catch (error) {
			console.error("Member search failed:", error);
			toast.error("Search failed. Please try again.");
		} finally {
			isSearching = false;
		}
	};

	const addExisting = async () => {
		if (!searchResult) return;
		if (!addEndDate) {
			toast.error("An end date is required.");
			return;
		}
		isAdding = true;
		try {
			await adminContractAddMember({ facilityId, id: contractId, body: { userId: searchResult.userId, endDate: toApiDate(addEndDate) } });
			toast.success("Member added");
			addOpen = false;
			void membersQuery.refresh();
		} catch (error) {
			console.error("Failed to add member:", error);
			toast.error("Failed to add member. They may already be a member.");
		} finally {
			isAdding = false;
		}
	};

	const createProfile = async (e: SubmitEvent) => {
		e.preventDefault();
		if (!createEmail.trim() || !createFirstName.trim() || !createLastName.trim() || !addEndDate) {
			toast.error("First name, last name, email and an end date are required.");
			return;
		}

		isCreating = true;
		try {
			await adminContractCreateMember({
				facilityId,
				id: contractId,
				body: {
					email: createEmail.trim(),
					firstName: createFirstName.trim(),
					lastName: createLastName.trim(),
					phoneNumber: createPhone.trim() || null,
					endDate: toApiDate(addEndDate),
				},
			});
			toast.success("Profile created and member added. They'll get an email to set their password.");
			addOpen = false;
			void membersQuery.refresh();
		} catch (error) {
			console.error("Failed to create member profile:", error);
			toast.error("Failed to create the profile. A user with this email may already exist.");
		} finally {
			isCreating = false;
		}
	};

	const openEdit = (member: AdminContractMemberDTO) => {
		editTarget = member;
		editEndDate = member.endDate ? toCalendarDate(member.endDate) : getDefaultEndDate();
	};

	const updateMembership = async (e: SubmitEvent) => {
		e.preventDefault();
		if (!editTarget || !editEndDate) return;

		isUpdating = true;
		try {
			await adminContractUpdateMember({
				facilityId,
				id: contractId,
				memberId: editTarget.id,
				body: { endDate: toApiDate(editEndDate) },
			});
			toast.success("Membership end date updated");
			editTarget = null;
			void membersQuery.refresh();
		} catch (error) {
			console.error("Failed to update membership:", error);
			toast.error("Failed to update the membership end date. Please try again.");
		} finally {
			isUpdating = false;
		}
	};

	const confirmRemove = async () => {
		if (!removeTarget) return;
		isRemoving = true;
		try {
			await adminContractRemoveMember({ facilityId, id: contractId, memberId: removeTarget.id });
			toast.success("Member removed");
			removeTarget = null;
			void membersQuery.refresh();
		} catch (error) {
			console.error("Failed to remove member:", error);
			toast.error("Failed to remove member. Please try again.");
		} finally {
			isRemoving = false;
		}
	};

	const columns: ColumnDef<DataTableFeatures, AdminContractMemberDTO>[] = [
		{
			header: "Member",
			id: "name",
			cell: (item) => `${item.row.original.firstName} ${item.row.original.lastName}`,
		},
		{ header: "Email", accessorKey: "email", cell: (item) => item.row.original.email ?? "—" },
		{
			header: "Period",
			id: "period",
			cell: (item) => `${formatDate(item.row.original.startDate)} – ${item.row.original.endDate ? formatDate(item.row.original.endDate) : "—"}`,
		},
		{
			header: "Status",
			id: "isActive",
			cell: (item) => renderSnippet(statusCell, item.row.original),
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
			return members;
		},
		enableRowSelection: false,
	});
</script>

{#snippet statusCell(member: AdminContractMemberDTO)}
	<Badge variant={member.isActive ? "default" : "secondary"}>{member.isActive ? "Active" : "Inactive"}</Badge>
{/snippet}

{#snippet actionsCell(member: AdminContractMemberDTO)}
	<Actions
		actions={[
			{ icon: PencilIcon, text: "Edit end date", action: () => openEdit(member) },
			{ icon: Trash2Icon, text: "Remove", action: () => (removeTarget = member) },
		]}
	/>
{/snippet}

<div class="m-4">
	<Button variant="ghost" size="sm" class="mb-2" href={contractsBackHref}>
		<ChevronLeftIcon class="size-4" />
		Back to contracts
	</Button>

	<PageHeading title="Manage members" description={contract ? `Members of ${contract.name}.` : "Members of this contract."} icon={UsersIcon} />

	<DataTable {table} headerClass="pb-2" isLoading={membersQuery.loading} noDataMessage="No members yet">
		{#snippet rightToolbar()}
			<Button size="sm" onclick={openAdd}>
				<PlusIcon class="size-4" />
				Add member
			</Button>
		{/snippet}
	</DataTable>
</div>

<Dialog.Root bind:open={addOpen}>
	<Dialog.Content class="sm:max-w-lg">
		<Dialog.Header>
			<Dialog.Title>Add member</Dialog.Title>
			<Dialog.Description>Search by email address or cellphone number to find an existing user.</Dialog.Description>
		</Dialog.Header>

		<form onsubmit={runSearch} class="flex items-end gap-2">
			<Field.Field class="flex-1">
				<Field.Label for="member-search">Email or cellphone</Field.Label>
				<Input id="member-search" bind:value={searchInput} placeholder="name@example.com or 0821234567" />
			</Field.Field>
			<Button type="submit" disabled={isSearching}>
				<SearchIcon class="size-4" />
				{isSearching ? "Searching..." : "Search"}
			</Button>
		</form>

		<Field.Field>
			<Field.Label for="member-end-date">Membership end date</Field.Label>
			<Popover.Root bind:open={addEndDatePickerOpen}>
				<div class="relative">
					<Input id="member-end-date" value={dateFormatter.format(addEndDate.toDate(getLocalTimeZone()))} readonly class="pr-9" />
					<Popover.Trigger>
						{#snippet child({ props })}
							<Button {...props} type="button" variant="ghost" size="icon" class="absolute end-1 top-1/2 size-7 -translate-y-1/2">
								<CalendarIcon class="size-3.5" />
								<span class="sr-only">Select membership end date</span>
							</Button>
						{/snippet}
					</Popover.Trigger>
				</div>
				<Popover.Content class="w-auto overflow-hidden p-0" align="start">
					<DatePickerCalendar bind:value={addEndDate} type="single" onValueChange={() => (addEndDatePickerOpen = false)} captionLayout="dropdown" />
				</Popover.Content>
			</Popover.Root>
		</Field.Field>

		{#if hasSearched && searchResult}
			<div class="mt-2 flex items-center justify-between rounded-md border p-4">
				<div>
					<p class="font-semibold">{searchResult.firstName} {searchResult.lastName}</p>
					<p class="text-muted-foreground text-sm">{searchResult.email ?? searchResult.phoneNumber ?? ""}</p>
				</div>
				{#if searchResult.isExistingMember}
					<Badge variant="secondary">Already a member</Badge>
				{:else}
					<Button onclick={addExisting} disabled={isAdding}>{isAdding ? "Adding..." : "Add member"}</Button>
				{/if}
			</div>
		{:else if hasSearched && !searchResult && !showCreate}
			<div class="mt-2 flex flex-col gap-3 rounded-md border p-4">
				<p class="text-muted-foreground text-sm">No user found for “{searchInput}”.</p>
				<Button variant="outline" onclick={() => (showCreate = true)}>
					<PlusIcon class="size-4" />
					Create a new profile
				</Button>
			</div>
		{/if}

		{#if showCreate}
			<form onsubmit={createProfile} class="mt-2 flex flex-col gap-4 rounded-md border p-4">
				<p class="text-sm font-medium">New profile</p>
				<div class="grid grid-cols-2 gap-4">
					<Field.Field>
						<Field.Label for="create-first">First name</Field.Label>
						<Input id="create-first" bind:value={createFirstName} required />
					</Field.Field>
					<Field.Field>
						<Field.Label for="create-last">Last name</Field.Label>
						<Input id="create-last" bind:value={createLastName} required />
					</Field.Field>
				</div>
				<Field.Field>
					<Field.Label for="create-email">Email</Field.Label>
					<Input id="create-email" type="email" bind:value={createEmail} required />
				</Field.Field>
				<Field.Field>
					<Field.Label for="create-phone">Cellphone (optional)</Field.Label>
					<Input id="create-phone" bind:value={createPhone} />
				</Field.Field>
				<p class="text-muted-foreground text-xs">The new member receives an email to set their password and verify their account through Keycloak.</p>
				<div class="flex justify-end gap-2">
					<Button type="button" variant="outline" onclick={() => (showCreate = false)} disabled={isCreating}>Back</Button>
					<Button type="submit" disabled={isCreating}>{isCreating ? "Creating..." : "Create & add"}</Button>
				</div>
			</form>
		{/if}

		<Dialog.Footer>
			<Button type="button" variant="outline" onclick={() => (addOpen = false)}>Close</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

<Dialog.Root open={editTarget !== null} onOpenChange={(open) => !open && (editTarget = null)}>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Edit membership</Dialog.Title>
			<Dialog.Description>Set the end date for {editTarget?.firstName} {editTarget?.lastName}'s membership.</Dialog.Description>
		</Dialog.Header>
		<form onsubmit={updateMembership} class="flex flex-col gap-4">
			<Field.Field>
				<Field.Label for="edit-member-end-date">Membership end date</Field.Label>
				<Popover.Root bind:open={editEndDatePickerOpen}>
					<div class="relative">
						<Input id="edit-member-end-date" value={dateFormatter.format(editEndDate.toDate(getLocalTimeZone()))} readonly class="pr-9" />
						<Popover.Trigger>
							{#snippet child({ props })}
								<Button {...props} type="button" variant="ghost" size="icon" class="absolute end-1 top-1/2 size-7 -translate-y-1/2">
									<CalendarIcon class="size-3.5" />
									<span class="sr-only">Select membership end date</span>
								</Button>
							{/snippet}
						</Popover.Trigger>
					</div>
					<Popover.Content class="w-auto overflow-hidden p-0" align="start">
						<DatePickerCalendar bind:value={editEndDate} type="single" onValueChange={() => (editEndDatePickerOpen = false)} captionLayout="dropdown" />
					</Popover.Content>
				</Popover.Root>
			</Field.Field>
			<Dialog.Footer>
				<Button type="button" variant="outline" onclick={() => (editTarget = null)} disabled={isUpdating}>Cancel</Button>
				<Button type="submit" disabled={isUpdating}>{isUpdating ? "Saving..." : "Save"}</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>

<AlertDialog.Root open={removeTarget !== null} onOpenChange={(o) => !o && (removeTarget = null)}>
	<AlertDialog.Content>
		<AlertDialog.Header>
			<AlertDialog.Title>Remove member?</AlertDialog.Title>
			<AlertDialog.Description>
				This removes <span class="font-semibold">{removeTarget?.firstName} {removeTarget?.lastName}</span> from this contract. Their user profile is not deleted.
			</AlertDialog.Description>
		</AlertDialog.Header>
		<AlertDialog.Footer>
			<AlertDialog.Cancel disabled={isRemoving}>Cancel</AlertDialog.Cancel>
			<AlertDialog.Action onclick={confirmRemove} disabled={isRemoving}>{isRemoving ? "Removing..." : "Remove"}</AlertDialog.Action>
		</AlertDialog.Footer>
	</AlertDialog.Content>
</AlertDialog.Root>
