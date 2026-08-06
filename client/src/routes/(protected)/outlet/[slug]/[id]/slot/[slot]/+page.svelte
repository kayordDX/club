<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import type { ResolvedPathname } from "$app/types";

	import { bookingCreate } from "$lib/api/remote/booking.remote";
	import { createPlayers } from "$lib/booking/players";
	import BookingDetailsForm from "$lib/components/booking/BookingDetailsForm.svelte";
	import { Button, Card, Empty } from "@kayord/ui";
	import { ChevronLeftIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";

	const slug = page.params.slug ?? "";
	const facilityId = Number(page.params.id) || 0;
	const slotId = page.params.slot ?? "";
	const slotCount = Math.max(1, Number(page.url.searchParams.get("slotCount")) || 1);
	const selectedDate = page.url.searchParams.get("date") ?? "";
	const facilityHref = (
		selectedDate ? `${resolve(`/outlet/${slug}/${facilityId}`)}?date=${selectedDate}` : resolve(`/outlet/${slug}/${facilityId}`)
	) as ResolvedPathname;

	let isSubmitting = $state(false);

	const handleSubmit = async ({
		players,
		extras,
	}: {
		players: { name: string; cellNo: string; email: string; contractId: string }[];
		extras: { id: number; amount: number }[];
	}) => {
		try {
			console.log("here I am", players, extras);
			isSubmitting = true;
			const bookings = players.map((player) => ({
				slotId,
				slotContractId: Number(player.contractId),
				name: player.name,
				cellphone: player.cellNo,
				email: player.email,
			}));

			const bookingResponse = await bookingCreate({
				bookings,
				extras: extras.map((extra) => ({
					extraId: extra.id,
					amount: extra.amount,
				})),
			});

			const paymentParams = new URLSearchParams({
				slotId,
				slotCount: slotCount.toString(),
				date: selectedDate,
			});
			toast.info("Created booking");
			await goto(`${resolve(`/outlet/${slug}/${facilityId}/booking/${bookingResponse.id}/pay`)}?${paymentParams.toString()}` as ResolvedPathname);
		} catch {
			toast.error("Failed to create booking. Please try again.");
		} finally {
			isSubmitting = false;
		}
	};
</script>

<div class="mx-auto flex w-full flex-col gap-6">
	<div class="grid gap-4 pt-4">
		{#if !selectedDate}
			<Card.Root class="border-border/60 overflow-hidden border shadow-sm">
				<Card.Content class="p-6">
					<Empty.Root>
						<Empty.Header>
							<Empty.Title>Select a date first</Empty.Title>
							<Empty.Description>Choose a date on the facility page before continuing with player details.</Empty.Description>
						</Empty.Header>
						<Empty.Content>
							<Button href={facilityHref} variant="outline">
								<ChevronLeftIcon class="size-4" />
								Back to slots
							</Button>
						</Empty.Content>
					</Empty.Root>
				</Card.Content>
			</Card.Root>
		{:else}
			<BookingDetailsForm
				title="Add each player's details"
				description={`Capture the booking information for all ${slotCount} players, review the summary, and then choose how you want to pay.`}
				submitLabel="Book"
				submittingLabel="Creating..."
				{isSubmitting}
				backHref={facilityHref}
				backLabel="Back to slots"
				{slotId}
				{facilityId}
				date={selectedDate}
				dateLabel={selectedDate}
				initialPlayers={createPlayers(slotCount)}
				initialExtras={[]}
				onSubmit={handleSubmit}
			/>
		{/if}
	</div>
</div>
