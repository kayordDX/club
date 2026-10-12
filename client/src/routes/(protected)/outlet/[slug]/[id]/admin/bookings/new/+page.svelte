<script lang="ts">
	import { onDestroy } from "svelte";
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import type { ResolvedPathname } from "$app/types";
	import { createMutation, createQuery, useQueryClient } from "@tanstack/svelte-query";
	import { BookingStatusEnum, type AdminBookingCustomerDTO } from "$lib/api";
	import type { BookingFormSubmitValues } from "$lib/booking/schema";
	import { adminBookingCreate, adminBookingGetOptions, adminBookingSearchCustomer, adminSlotGetAll } from "$lib/api/remote/admin.remote";
	import { slotGetAll } from "$lib/api/remote/slot.remote";
	import { createPlayers } from "$lib/booking/players";
	import type { BookingFormSubmitHandler } from "$lib/booking/schema";
	import BookingDetailsForm from "$lib/components/booking/BookingDetailsForm.svelte";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Alert, Button, Card, Input } from "@kayord/ui";
	import { BookIcon, SearchIcon, UserRoundCheckIcon, XIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";

	const facilityId = $derived(Number(page.params.id) || 0);
	const slug = $derived(page.params.slug ?? "");
	const slotId = $derived(page.url.searchParams.get("slotId") ?? "");
	const date = $derived(page.url.searchParams.get("date") ?? "");
	const slotCount = $derived(Math.max(1, Math.min(24, Number(page.url.searchParams.get("slotCount")) || 1)));
	const routeParams = $derived({ slug, id: String(facilityId) });
	const bookingsHref = $derived(resolve("/(protected)/outlet/[slug]/[id]/admin/bookings", routeParams));
	const scheduleHref = $derived(
		`${resolve("/(protected)/outlet/[slug]/[id]/admin/schedule", routeParams)}?date=${encodeURIComponent(date)}` as ResolvedPathname
	);

	let customerSearchInput = $state("");
	let customerSearch = $state("");
	let selectedCustomer = $state.raw<AdminBookingCustomerDTO | null>(null);
	let createError = $state("");
	let createdBookingId = $state<number | null>(null);
	let recoveryError = $state("");

	let searchTimer: ReturnType<typeof setTimeout> | undefined;
	onDestroy(() => clearTimeout(searchTimer));

	function updateCustomerSearch(value: string) {
		customerSearchInput = value;
		clearTimeout(searchTimer);
		searchTimer = setTimeout(() => (customerSearch = value.trim()), 300);
	}

	const customerLookup = createQuery(() => ({
		queryKey: ["admin-booking-customer", facilityId, customerSearch],
		enabled: customerSearch.length >= 3 && customerSearch.length <= 256 && facilityId > 0,
		queryFn: async () => {
			const result = adminBookingSearchCustomer({ facilityId, params: { query: customerSearch } });
			await result.refresh();
			return (await result) ?? null;
		},
	}));
	const options = createQuery(() => ({
		queryKey: ["admin-booking-options", facilityId, slotId, selectedCustomer?.userId],
		enabled: Boolean(slotId && selectedCustomer?.userId && facilityId > 0),
		queryFn: async () => {
			const result = adminBookingGetOptions({ facilityId, params: { slotId, userId: selectedCustomer!.userId } });
			await result.refresh();
			return await result;
		},
	}));
	const createBooking = createMutation(() => ({
		mutationFn: (body: Parameters<typeof adminBookingCreate>[0]["body"]) => adminBookingCreate({ facilityId, body }),
	}));
	const queryClient = useQueryClient();
	const searchIsSettled = $derived(customerSearchInput.trim() === customerSearch);
	const customerName = $derived(
		selectedCustomer
			? [selectedCustomer.firstName, selectedCustomer.lastName].filter(Boolean).join(" ") ||
					selectedCustomer.email ||
					selectedCustomer.phoneNumber ||
					"Selected customer"
			: ""
	);

	function chooseCustomer() {
		if (!searchIsSettled || !customerLookup.data || createBooking.isPending || createdBookingId !== null) return;
		selectedCustomer = customerLookup.data;
		createError = "";
	}

	function clearCustomer() {
		if (createBooking.isPending || createdBookingId !== null) return;
		selectedCustomer = null;
	}

	async function openCreatedBooking() {
		if (!createdBookingId) return;
		await goto(
			resolve("/(protected)/outlet/[slug]/[id]/admin/bookings/[bookingId]", {
				...routeParams,
				bookingId: String(createdBookingId),
			})
		);
	}

	const handleSubmit: BookingFormSubmitHandler = async ({ players, extras }: BookingFormSubmitValues) => {
		if (!selectedCustomer || !options.data || createBooking.isPending || createdBookingId !== null) return;
		createError = "";
		try {
			const result = await createBooking.mutateAsync({
				userId: selectedCustomer.userId,
				bookingStatusId: BookingStatusEnum.Confirmed,
				bookings: players.map((player) => ({
					slotId,
					slotContractId: Number(player.contractId),
					name: player.name,
					cellphone: player.cellNo,
					email: player.email,
				})),
				extras: extras.map((extra) => ({ extraId: extra.id, amount: extra.amount })),
			});
			createdBookingId = result.id;
			toast.success("Booking created");
			const dateTime = `${date}T00:00:00.000Z`;
			const refreshed = await Promise.allSettled([
				queryClient.invalidateQueries({ queryKey: ["admin-bookings", facilityId] }),
				queryClient.invalidateQueries({ queryKey: ["admin-slots", facilityId] }),
				adminSlotGetAll({ facilityId, params: { date: dateTime } }).refresh(),
				slotGetAll({ facilityId, date: dateTime }).refresh(),
			]);
			if (refreshed.some((item) => item.status === "rejected")) {
				recoveryError =
					"The booking was created, but availability could not be refreshed. Open its admin details, then refresh the schedule before creating another booking.";
				return;
			}
			try {
				await openCreatedBooking();
			} catch {
				recoveryError = "The booking was created, but its details could not be opened. Use the link below to continue; do not submit again.";
			}
		} catch (cause) {
			if (createdBookingId !== null) {
				recoveryError = "The booking was created, but its details could not be opened. Use the link below to continue; do not submit again.";
				return;
			}
			createError = cause instanceof Error ? cause.message : "Unable to create booking. Please try again.";
		}
	};
</script>

<div class="m-4 space-y-6">
	<div class="flex flex-wrap items-start justify-between gap-4">
		<PageHeading title="Create booking" description="Create a booking for an existing customer." icon={BookIcon} />
		<div class="flex gap-2">
			<Button href={scheduleHref} variant="outline">Back to schedule</Button><Button href={bookingsHref} variant="outline">Back to bookings</Button>
		</div>
	</div>

	{#if !slotId || !date}
		<Alert.Root variant="destructive"
			><Alert.Title>Slot information missing</Alert.Title><Alert.Description
				>Start booking creation from an available slot in the facility schedule.</Alert.Description
			></Alert.Root
		>
	{:else}
		<Card.Root>
			<Card.Header
				><Card.Title>Booking owner</Card.Title><Card.Description>Search by the customer's registered email or phone number.</Card.Description></Card.Header
			>
			<Card.Content class="space-y-4">
				{#if selectedCustomer}
					<div class="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-4">
						<div class="flex items-center gap-2">
							<UserRoundCheckIcon class="size-4" /><span class="font-medium">{customerName}</span><span class="text-muted-foreground"
								>{selectedCustomer.email ?? selectedCustomer.phoneNumber}</span
							>
						</div>
						<Button variant="outline" size="sm" onclick={clearCustomer}><XIcon class="size-4" />Choose another</Button>
					</div>
				{:else}
					<div class="flex max-w-2xl gap-2">
						<div class="relative grow">
							<SearchIcon class="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" /><Input
								bind:value={() => customerSearchInput, updateCustomerSearch}
								type="search"
								placeholder="Email or phone number"
								aria-label="Search customers"
								class="pl-9"
							/>
						</div>
						<Button onclick={chooseCustomer} disabled={!searchIsSettled || !customerLookup.data || customerLookup.isFetching}
							><SearchIcon class="size-4" />Select customer</Button
						>
					</div>
					{#if customerLookup.isFetching}<p class="text-muted-foreground text-sm">Searching registered customers…</p>
					{:else if searchIsSettled && customerSearch && customerLookup.isError}<Alert.Root variant="destructive"
							><Alert.Title>Customer search failed</Alert.Title><Alert.Description>{customerLookup.error.message}</Alert.Description></Alert.Root
						>
					{:else if searchIsSettled && customerSearch && customerLookup.data === null}<p class="text-muted-foreground text-sm">
							No registered customer found. Check the email or phone number.
						</p>
					{:else if searchIsSettled && customerLookup.data}<Button
							variant="outline"
							class="flex h-auto w-full max-w-2xl items-center justify-between p-3 text-left"
							onclick={chooseCustomer}
							><span
								>{[customerLookup.data.firstName, customerLookup.data.lastName].filter(Boolean).join(" ") || "Registered customer"}<span
									class="text-muted-foreground ml-2">{customerLookup.data.email ?? customerLookup.data.phoneNumber}</span
								></span
							><span class="text-sm">Select</span></Button
						>{/if}
				{/if}
			</Card.Content>
		</Card.Root>

		{#if createdBookingId !== null}
			<Alert.Root variant={recoveryError ? "destructive" : "default"}>
				<Alert.Title>Booking #{createdBookingId} created</Alert.Title>
				<Alert.Description>{recoveryError || "Opening admin booking details…"}</Alert.Description>
				{#if recoveryError}<Button
						onclick={() => openCreatedBooking().catch(() => (recoveryError = "Unable to open booking details. Retry or use the bookings list."))}
						>Open booking details</Button
					><Button variant="outline" href={scheduleHref}>Back to schedule</Button><Button variant="outline" href={bookingsHref}>Back to bookings</Button>{/if}
			</Alert.Root>
		{:else if selectedCustomer}
			{#if options.isError}<Alert.Root variant="destructive"
					><Alert.Title>Booking options unavailable</Alert.Title><Alert.Description>{options.error.message}</Alert.Description></Alert.Root
				>
			{:else if options.isPending || !options.data}<p class="text-muted-foreground">Loading this customer's eligible contracts and extras…</p>
			{:else if options.data.contracts.length === 0}<Alert.Root variant="destructive"
					><Alert.Title>No eligible contracts</Alert.Title><Alert.Description>This customer has no eligible contracts for the selected slot.</Alert.Description
					></Alert.Root
				>
			{:else}
				{#if createError}<Alert.Root variant="destructive"
						><Alert.Title>Booking not created</Alert.Title><Alert.Description>{createError}</Alert.Description></Alert.Root
					>{/if}
				{#key `${facilityId}:${slotId}:${date}:${slotCount}:${selectedCustomer.userId}:${JSON.stringify(options.data)}`}
					<BookingDetailsForm
						title="Booking details"
						description={`Enter player details and extras for ${customerName}. Review the total before creating.`}
						submitLabel="Create booking"
						submittingLabel="Creating…"
						isSubmitting={createBooking.isPending}
						backHref={bookingsHref}
						backLabel="Back to bookings"
						{slotId}
						{facilityId}
						{date}
						dateLabel={date}
						initialPlayers={createPlayers(slotCount).map((player, index) =>
							index === 0
								? {
										...player,
										name: [selectedCustomer?.firstName, selectedCustomer?.lastName].filter(Boolean).join(" "),
										cellNo: selectedCustomer?.phoneNumber ?? "",
										email: selectedCustomer?.email ?? "",
									}
								: player
						)}
						initialExtras={[]}
						profileShortcutUser={{
							name: [selectedCustomer.firstName, selectedCustomer.lastName].filter(Boolean).join(" "),
							phone_number: selectedCustomer.phoneNumber,
							email: selectedCustomer.email,
						}}
						availableContracts={options.data.contracts}
						availableExtras={options.data.extras}
						reviewStatus="Confirmed"
						onSubmit={handleSubmit}
					/>
				{/key}
			{/if}
		{/if}
	{/if}
</div>
