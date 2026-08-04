<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { goto } from "$app/navigation";
	import { useQueryClient } from "@tanstack/svelte-query";

	import { BookingStatusEnum, createBookingGet, createBookingGetPath, createBookingUpdate } from "$lib/api";
	import { getBookingPayUrl } from "$lib/booking/payUrl";
	import { formatDate } from "$lib/booking/format";
	import BookingBreadcrumbs from "$lib/components/BookingBreadcrumbs.svelte";
	import BookingDetailsForm from "$lib/components/booking/BookingDetailsForm.svelte";
	import CountdownTimer from "$lib/components/CountdownTimer.svelte";
	import Query from "$lib/components/Query.svelte";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Alert, Badge, Button } from "@kayord/ui";
	import { ChevronLeftIcon, PencilIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { buildExtras, buildPlayers } from "$lib/booking/bookingForm";

	const bookingId = $derived(Number(page.params.id) || 0);
	const query = createBookingGet(() => bookingId);
	const booking = $derived(query.data);
	const isPending = $derived(booking?.bookingStatus?.id === BookingStatusEnum.Pending);

	const pathQuery = createBookingGetPath(
		() => bookingId,
		() => ({ query: { enabled: bookingId > 0 } })
	);
	const path = $derived(pathQuery.data);
	const bookingHref = $derived(resolve(`/bookings/${bookingId}`));

	const slotId = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slotId ?? "");
	const slotStartDatetime = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime ?? null);
	const slotEndDatetime = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slot?.endDatetime ?? null);
	const slotDate = $derived(slotStartDatetime?.slice(0, 10) ?? "");
	const facilityId = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slot?.facilityId ?? 0);
	const ownPlayerCount = $derived(booking?.slotContractBookings?.length ?? 0);

	const updateMutation = createBookingUpdate();
	const queryClient = useQueryClient();

	const handleSubmit = async ({
		players,
		extras,
	}: {
		players: { name: string; cellNo: string; email: string; contractId: string }[];
		extras: { id: number; amount: number }[];
	}) => {
		try {
			await updateMutation.mutateAsync({
				id: bookingId,
				data: {
					bookings: players.map((player) => ({
						slotId,
						slotContractId: Number(player.contractId),
						name: player.name,
						cellphone: player.cellNo,
						email: player.email,
					})),
					extras: extras.map((extra) => ({
						extraId: extra.id,
						amount: extra.amount,
					})),
				},
			});

			toast.success("Booking updated");
			queryClient.invalidateQueries({ queryKey: [`/booking/${bookingId}`] });
			queryClient.invalidateQueries({ queryKey: ["/booking/user"] });
			const nextPayUrl = path ? getBookingPayUrl(bookingId, path, players.length) : null;
			await goto(nextPayUrl ?? bookingHref);
		} catch (error) {
			const message =
				error instanceof Error && error.cause ? String(error.cause) : error instanceof Error ? error.message : "Failed to update booking. Please try again.";
			toast.error(message);
		}
	};
</script>

<Query {query} emptyText="Booking not found">
	<BookingBreadcrumbs {bookingId} {pathQuery} slotCount={ownPlayerCount}>
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
					<BookingDetailsForm
						title="Edit booking details"
						description="Update the player details, review the summary, and save your changes. Payment can be completed from the pay page."
						submitLabel="Save changes"
						submittingLabel="Saving..."
						isSubmitting={updateMutation.isPending}
						backHref={bookingHref}
						backLabel="Back to booking"
						{slotId}
						{facilityId}
						date={slotDate}
						dateLabel={formatDate(slotStartDatetime)}
						{slotStartDatetime}
						{slotEndDatetime}
						initialPlayers={buildPlayers(booking)}
						initialExtras={buildExtras(booking)}
						{ownPlayerCount}
						onSubmit={handleSubmit}
					>
						{#snippet headerExtra()}
							{#if booking?.expiresAt}
								<CountdownTimer expiresAt={booking.expiresAt} />
							{/if}
						{/snippet}
						{#snippet statusExtra()}
							<Badge variant="outline">Status: {booking?.bookingStatus?.name}</Badge>
						{/snippet}
					</BookingDetailsForm>
				</div>
			{/if}
		</div>
	</BookingBreadcrumbs>
</Query>
