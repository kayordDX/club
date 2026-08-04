<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { goto } from "$app/navigation";
	import { useQueryClient } from "@tanstack/svelte-query";
	import { BookingStatusEnum, createBookingGet, createBookingGetPath, createBookingUpdate, createSlotGetContracts } from "$lib/api";
	import { createAppForm, Form } from "$lib/components/Form";
	import Query from "$lib/components/Query.svelte";
	import BookingBreadcrumbs from "$lib/components/BookingBreadcrumbs.svelte";
	import { getBookingPayUrl } from "$lib/booking/payUrl";
	import PageHeading from "../../../settings/PageHeading.svelte";
	import { Alert, Badge, Button, Card } from "@kayord/ui";
	import { CalendarDaysIcon, ChevronLeftIcon, Clock3Icon, CreditCardIcon, PencilIcon, UserRoundIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { playersSchema, type Players, type SelectedExtra } from "../../../outlet/[slug]/[id]/slot/[slot]/schema";
	import Extras from "../../../outlet/[slug]/[id]/slot/[slot]/Extras.svelte";
	import { getSelectedExtrasTotal } from "../../../outlet/[slug]/[id]/slot/[slot]/pricing";
	import { buildExtras, buildPlayers } from "./bookingForm";

	const bookingId = $derived(Number(page.params.id) || 0);
	const query = createBookingGet(() => bookingId);
	const booking = $derived(query.data);
	const isPending = $derived(booking?.bookingStatus?.id === BookingStatusEnum.Pending);

	const pathQuery = createBookingGetPath(
		() => bookingId,
		() => ({ query: { enabled: bookingId > 0 } })
	);
	const path = $derived(pathQuery.data);
	const slotCount = $derived(booking?.slotContractBookings?.length ?? 0);
	const bookingHref = $derived(resolve(`/bookings/${bookingId}`));

	const slotId = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slotId ?? "");
	const contractsQuery = createSlotGetContracts(
		() => slotId,
		() => ({ query: { enabled: !!slotId } })
	);
	const contracts = $derived(contractsQuery.data ?? []);
	const contractItems = $derived(
		contracts.map((contract) => ({
			value: contract.id.toString(),
			label: `${contract.contractName} ${contract.description} - R ${contract.price.toFixed(2)}`,
		}))
	);

	let selectedExtras: Array<SelectedExtra> = $state([]);
	let hydrated = $state(false);
	$effect(() => {
		if (booking && !hydrated) {
			hydrated = true;
			selectedExtras = buildExtras(booking);
		}
	});

	const updateMutation = createBookingUpdate();
	const queryClient = useQueryClient();

	const form = createAppForm(() => ({
		defaultValues: {
			players: buildPlayers(booking),
		} satisfies Players,
		validators: {
			onChange: playersSchema,
		},
		onSubmit: async ({ value }) => {
			try {
				await updateMutation.mutateAsync({
					id: bookingId,
					data: {
						bookings: value.players.map((player) => ({
							slotId,
							slotContractId: Number(player.contractId),
							name: player.name,
							cellphone: player.cellNo,
							email: player.email,
						})),
						extras: selectedExtras.map((extra) => ({
							extraId: extra.id,
							amount: extra.amount,
						})),
					},
				});

				toast.success("Booking updated");
				queryClient.invalidateQueries({ queryKey: [`/booking/${bookingId}`] });
				queryClient.invalidateQueries({ queryKey: ["/booking/user"] });
				const nextPayUrl = path ? getBookingPayUrl(bookingId, path, value.players.length) : null;
				await goto(nextPayUrl ?? bookingHref);
			} catch {
				toast.error("Failed to update booking. Please try again.");
			}
		},
	}));

	const players = form.useStore((state) => state.values.players);

	const totalPrice = $derived(
		(players.current ?? [])
			.map((player) => contracts.find((contract) => contract.id === Number(player.contractId))?.price ?? 0)
			.reduce((sum, price) => sum + price, 0) + getSelectedExtrasTotal(selectedExtras)
	);

	const formatCurrency = (value: number) =>
		new Intl.NumberFormat("en-ZA", {
			style: "currency",
			currency: "ZAR",
		}).format(value);

	const formatTime = (datetime?: string | null) => {
		if (!datetime) return "";
		return new Date(datetime).toLocaleTimeString("en-ZA", {
			hour: "2-digit",
			minute: "2-digit",
			hour12: false,
		});
	};
</script>

<Query {query} emptyText="Booking not found">
	<BookingBreadcrumbs {bookingId} {pathQuery} {slotCount}>
		<div class="m-2">
			<PageHeading title="Edit Booking" description={`Booking #${bookingId}`} icon={PencilIcon} />

			{#if !isPending}
				<div class="mt-8">
					<Alert.Root variant="default">
						<Alert.Title>Booking can't be edited</Alert.Title>
						<Alert.Description>
							Only pending bookings can be edited. This booking is currently {booking?.bookingStatus?.name ?? "unknown"}.
						</Alert.Description>
					</Alert.Root>

					<div class="mt-4 flex gap-2">
						<Button href={bookingHref} variant="outline">
							<ChevronLeftIcon class="size-4" />
							Back to booking
						</Button>
					</div>
				</div>
			{:else}
				<div class="mx-auto mt-4 flex w-full flex-col gap-6">
					<Card.Root class="border-border/60 overflow-hidden border shadow-sm">
						<Card.Header class="border-border/60 border-b">
							<div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
								<div class="space-y-2">
									<Card.Title class="text-2xl">Edit booking details</Card.Title>
									<Card.Description class="max-w-2xl text-sm leading-6">
										Update the player details, review the summary, and save your changes. Payment can be completed from the pay page.
									</Card.Description>
								</div>
								<Badge variant="outline">Status: {booking?.bookingStatus?.name}</Badge>
							</div>
						</Card.Header>

						<Form {form}>
							<Card.Content class="space-y-6 p-6">
								<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
									<div class="rounded-2xl border p-4">
										<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
											<CalendarDaysIcon class="size-4" />
											Date
										</div>
										<p class="mt-3 text-sm font-semibold">
											{booking?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime?.slice(0, 10) ?? "—"}
										</p>
									</div>
									<div class="rounded-2xl border p-4">
										<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
											<Clock3Icon class="size-4" />
											Time
										</div>
										<p class="mt-3 text-sm font-semibold">
											{formatTime(booking?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)} - {formatTime(
												booking?.slotContractBookings?.[0]?.slotContract?.slot?.endDatetime
											)}
										</p>
									</div>
									<div class="rounded-2xl border p-4">
										<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
											<UserRoundIcon class="size-4" />
											Players
										</div>
										<p class="mt-3 text-sm font-semibold">{players.current?.length ?? 0} total</p>
									</div>
									<div class="rounded-2xl border p-4">
										<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
											<CreditCardIcon class="size-4" />
											Total
										</div>
										<p class="mt-3 text-sm font-semibold">{formatCurrency(totalPrice)}</p>
									</div>
								</div>

								<div class="space-y-4">
									<div class="flex items-center justify-between gap-4">
										<div>
											<h2 class="text-lg font-semibold">User information</h2>
											<p class="text-muted-foreground text-sm">Update the contact details for each user included in this booking.</p>
										</div>
										<Badge variant="outline">{players.current?.length ?? 0} users</Badge>
									</div>

									<div class="space-y-4">
										<form.Field name="players">
											{#snippet children(field)}
												<div class="flex flex-col gap-4">
													{#each field.state.value as player, index (index)}
														<Card.Root>
															<Card.Header class="pb-4">
																<Card.Title class="text-base">{player.name || `User ${index + 1}`}</Card.Title>
															</Card.Header>
															<Card.Content>
																<div class="grid gap-4 md:grid-cols-2">
																	<form.AppField name={`players[${index}].contractId`}>
																		{#snippet children(field)}
																			<field.Select label="Contract" items={contractItems} />
																		{/snippet}
																	</form.AppField>
																	<form.AppField name={`players[${index}].name`}>
																		{#snippet children(field)}
																			<field.Input label="Name" placeholder="Player full name" />
																		{/snippet}
																	</form.AppField>
																	<form.AppField name={`players[${index}].cellNo`}>
																		{#snippet children(field)}
																			<field.Input label="Cell No" placeholder="e.g. 082 123 4567" />
																		{/snippet}
																	</form.AppField>
																	<form.AppField name={`players[${index}].email`}>
																		{#snippet children(field)}
																			<field.Input label="Email" type="text" placeholder="player@email.com" />
																		{/snippet}
																	</form.AppField>
																</div>
															</Card.Content>
														</Card.Root>
													{/each}
												</div>
											{/snippet}
										</form.Field>
									</div>
								</div>

								<div class="space-y-4">
									<div class="flex items-center justify-between gap-4">
										<div>
											<h2 class="text-lg font-semibold">Extras</h2>
											<p class="text-muted-foreground text-sm">Add or remove extras for this booking.</p>
										</div>
									</div>
									<Extras facilityId={booking?.slotContractBookings?.[0]?.slotContract?.slot?.facilityId ?? 0} bind:selectedExtras />
								</div>
							</Card.Content>
							<Card.Footer class="flex justify-between border-t">
								<Button href={bookingHref} variant="ghost">
									<ChevronLeftIcon class="size-4" />
									Back to booking
								</Button>
								<div class="flex items-center gap-3">
									<span class="text-muted-foreground text-sm">
										Total: {formatCurrency(totalPrice)}
									</span>
									<Button type="submit" disabled={updateMutation.isPending}>
										{updateMutation.isPending ? "Saving..." : "Save changes"}
									</Button>
								</div>
							</Card.Footer>
						</Form>
					</Card.Root>
				</div>
			{/if}
		</div>
	</BookingBreadcrumbs>
</Query>
