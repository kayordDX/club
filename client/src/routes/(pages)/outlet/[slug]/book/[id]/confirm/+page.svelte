<script lang="ts">
	import { page } from "$app/state";
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { toast } from "svelte-sonner";
	import { createQuery } from "@tanstack/svelte-query";
	import { Breadcrumb, Button, Card, Input, Loader } from "@kayord/ui";
	import {
		CalendarClockIcon,
		ChevronLeftIcon,
		CircleAlertIcon,
		Clock3Icon,
		MailIcon,
		UserIcon,
	} from "@lucide/svelte";
	import { createOutletGet, createSlotGetAll, createSlotGetContracts } from "$lib/api";
	import { bookingCreate } from "$lib/api/generated/booking";
	import Query from "$lib/components/Query.svelte";
	import { getError } from "$lib/types";
	import { user } from "$lib/stores/user.svelte";
	import { bookingFlow, type BookingPlayer } from "$lib/stores/booking-flow.svelte";
	import LoginButton from "$lib/components/LoginButton/LoginButton.svelte";

	const slotId = $derived(page.url.searchParams.get("slotId") ?? "");
	const selectedDate = $derived(page.url.searchParams.get("date") ?? new Date().toISOString().slice(0, 10));
	const quantity = $derived.by(() => {
		const requestedQuantity = Number(page.url.searchParams.get("quantity") ?? "1");
		return Number.isFinite(requestedQuantity) && requestedQuantity > 0 ? requestedQuantity : 1;
	});

	const outletQuery = createOutletGet(() => page.params.slug ?? "");
	const outlet = $derived(outletQuery.data);
	const facility = $derived(outlet?.facilities.find((item) => item.id === Number(page.params.id)));

	const slotsQuery = createSlotGetAll(() => ({
		facilityId: Number(page.params.id),
		date: selectedDate,
	}));
	const selectedSlot = $derived(slotsQuery.data?.find((slot) => slot.id === slotId));

	const contractsQuery = createSlotGetContracts(
		() => slotId,
		() => ({
			query: {
				enabled: slotId.length > 0,
			},
		})
	);
	const contracts = $derived(contractsQuery.data ?? []);
	const selectedContract = $derived(contracts[0]);
	const totalPrice = $derived((selectedContract?.price ?? 0) * quantity);

	const backHref = $derived(
		resolve(`/outlet/${page.params.slug}/book/${page.params.id}?date=${encodeURIComponent(selectedDate)}`)
	);
	const nextHref = $derived(resolve(`/outlet/${page.params.slug}/book/${page.params.id}/booking`));
	const loginRequired = $derived(Boolean(selectedSlot?.requiresLogin));
	const guestBookingBlocked = $derived(Boolean(selectedSlot && !selectedSlot.canBookForGuests));
	const needsLogin = $derived((loginRequired || guestBookingBlocked) && !user.isLoggedIn);
	const slotUnavailable = $derived(!slotsQuery.isLoading && slotId.length > 0 && !selectedSlot);
	const canCreatePendingBooking = $derived(
		!slotUnavailable && Boolean(selectedSlot && selectedContract) && !needsLogin
	);

	let players = $state<BookingPlayer[]>([]);

	$effect(() => {
		if (players.length === quantity) return;

		players = Array.from({ length: quantity }, (_, index) => ({
			name: players[index]?.name ?? "",
			email: players[index]?.email ?? (index === 0 ? (user.value?.email ?? "") : ""),
		}));
	});

	const pendingBookingQuery = createQuery(() => ({
		queryKey: ["pending-booking", slotId, selectedContract?.id, quantity],
		enabled: canCreatePendingBooking,
		retry: false,
		staleTime: Infinity,
		queryFn: () =>
			bookingCreate({
				slotId,
				slotContractId: selectedContract!.id,
				quantity,
				email: user.value?.email ?? "pending@guest.local",
			}),
	}));

	const pendingBooking = $derived(bookingFlow.state?.booking ?? pendingBookingQuery.data);
	const bookingError = $derived(
		pendingBookingQuery.isError ? getError(pendingBookingQuery.error).message : undefined
	);
	const expiresAt = $derived(pendingBooking?.expiresAt);

	const isPlayerDetailsComplete = $derived(
		players.length === quantity &&
			players.every((player) => player.name.trim().length > 0 && player.email.trim().length > 0)
	);

	const formatDateTime = (value?: string | null) => {
		if (!value) return "";
		return new Date(value).toLocaleString("en-ZA", {
			dateStyle: "medium",
			timeStyle: "short",
		});
	};

	const goNext = async () => {
		if (!pendingBooking) {
			toast.error("Wait for the pending booking to finish being created.");
			return;
		}

		if (!isPlayerDetailsComplete) {
			toast.error("Add a name and email for each player.");
			return;
		}

		bookingFlow.set(pendingBooking, players);
		await goto(nextHref);
	};
</script>

<div class="m-4 space-y-4">
	<Query query={outletQuery} emptyText="Unable to load booking details">
		<div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
			<Breadcrumb.Root>
				<Breadcrumb.List>
					<Breadcrumb.Item>
						<Breadcrumb.Link href="/">Home</Breadcrumb.Link>
					</Breadcrumb.Item>
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						<Breadcrumb.Link href={resolve(`/outlet/${page.params.slug}/book`)}>
							{outlet?.name}
						</Breadcrumb.Link>
					</Breadcrumb.Item>
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						<Breadcrumb.Link href={backHref}>{facility?.name}</Breadcrumb.Link>
					</Breadcrumb.Item>
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						<Breadcrumb.Page>Confirm Booking</Breadcrumb.Page>
					</Breadcrumb.Item>
				</Breadcrumb.List>
			</Breadcrumb.Root>

			<Button variant="outline" href={backHref} class="self-start">
				<ChevronLeftIcon /> Back to slots
			</Button>
		</div>

		<Card.Root>
			<Card.Header>
				<Card.Title class="flex items-center gap-2">
					<CalendarClockIcon class="size-5" />
					Confirm Booking
				</Card.Title>
				<Card.Description>
					We validate the slot first and create a pending booking hold before you continue.
				</Card.Description>
			</Card.Header>
			<Card.Content class="space-y-6">
				{#if slotsQuery.isLoading || contractsQuery.isLoading}
					<Loader />
				{:else if slotUnavailable}
					<div class="border-border bg-muted/30 rounded-lg border p-4 text-sm">
						<div class="flex items-center gap-2 font-medium">
							<CircleAlertIcon class="size-4" />
							This slot is no longer available.
						</div>
						<p class="text-muted-foreground mt-2">
							Go back and choose another slot.
						</p>
					</div>
				{:else if needsLogin}
					<div class="space-y-4 rounded-lg border p-4">
						<div class="flex items-center gap-2 font-medium">
							<CircleAlertIcon class="size-4" />
							Login required
						</div>
						<p class="text-muted-foreground text-sm">
							This slot can only be held by a signed-in user.
						</p>
						<LoginButton />
					</div>
				{:else}
					<div class="grid gap-4 rounded-lg border p-4 lg:grid-cols-2">
						<div class="space-y-3">
							<div>
								<div class="text-muted-foreground text-xs">Resource</div>
								<div class="font-semibold">{selectedSlot?.resourceName}</div>
							</div>
							<div>
								<div class="text-muted-foreground text-xs">Start</div>
								<div class="font-semibold">{formatDateTime(selectedSlot?.startDatetime)}</div>
							</div>
							<div>
								<div class="text-muted-foreground text-xs">Finish</div>
								<div class="font-semibold">{formatDateTime(selectedSlot?.endDatetime)}</div>
							</div>
							<div>
								<div class="text-muted-foreground text-xs">Players</div>
								<div class="font-semibold">{quantity}</div>
							</div>
						</div>

						<div class="space-y-3 text-sm">
							<div class="flex items-center justify-between gap-3">
								<span class="text-muted-foreground">Facility</span>
								<span class="font-medium">{facility?.name}</span>
							</div>
							<div class="flex items-center justify-between gap-3">
								<span class="text-muted-foreground">Option</span>
								<span class="font-medium">{selectedContract?.contractName}</span>
							</div>
							<div class="flex items-center justify-between gap-3">
								<span class="text-muted-foreground">Per player</span>
								<span class="font-medium">R{(selectedContract?.price ?? 0).toFixed(2)}</span>
							</div>
							<div class="flex items-center justify-between gap-3 border-t pt-3 text-base">
								<span class="font-medium">Total</span>
								<span class="font-semibold">R{totalPrice.toFixed(2)}</span>
							</div>
						</div>
					</div>

					<div class="bg-muted/40 flex items-start gap-2 rounded-lg p-3 text-sm">
						<Clock3Icon class="mt-0.5 size-4 shrink-0" />
						<p>
							{#if pendingBookingQuery.isPending}
								We are creating your pending booking hold now.
							{:else}
								Your slot is held in pending state for 10 minutes while you complete the next step.
							{/if}
							{#if expiresAt}
								 Held until {formatDateTime(expiresAt)}.
							{/if}
						</p>
					</div>

					{#if bookingError}
						<div class="text-destructive rounded-lg border border-current/20 p-3 text-sm">
							{bookingError}
						</div>
					{/if}

					<div class="space-y-3">
						<div>
							<div class="font-semibold">Player Details</div>
							<div class="text-muted-foreground text-sm">
								Add a name and email for each player before continuing.
							</div>
						</div>

						<div class="grid gap-3">
							{#each players as player, index (index)}
								<div class="grid gap-3 rounded-lg border p-4 md:grid-cols-2">
									<div class="space-y-2">
										<label class="text-sm font-medium" for={`player-name-${index}`}>
											Player {index + 1} Name
										</label>
										<div class="relative">
											<UserIcon class="text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2" />
											<Input
												id={`player-name-${index}`}
												placeholder="Full name"
												class="pl-9"
												bind:value={player.name}
											/>
										</div>
									</div>

									<div class="space-y-2">
										<label class="text-sm font-medium" for={`player-email-${index}`}>
											Player {index + 1} Email
										</label>
										<div class="relative">
											<MailIcon class="text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2" />
											<Input
												id={`player-email-${index}`}
												type="email"
												placeholder="you@example.com"
												class="pl-9"
												bind:value={player.email}
											/>
										</div>
									</div>
								</div>
							{/each}
						</div>
					</div>
				{/if}
			</Card.Content>
			<Card.Footer>
				<Button
					class="w-full sm:w-auto"
					disabled={!pendingBooking || pendingBookingQuery.isPending || !isPlayerDetailsComplete}
					onclick={goNext}
				>
					{#if pendingBookingQuery.isPending}
						<Loader class="mr-2" />
					{/if}
					Next
				</Button>
			</Card.Footer>
		</Card.Root>
	</Query>
</div>
