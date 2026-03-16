<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { Button, Card } from "@kayord/ui";
	import { bookingFlow } from "$lib/stores/booking-flow.svelte";
	import { CircleAlertIcon, ChevronLeftIcon, ReceiptTextIcon, UsersIcon } from "@lucide/svelte";

	const confirmHref = $derived(
		resolve(`/outlet/${page.params.slug}/book/${page.params.id}/confirm${page.url.search}`)
	);
</script>

{#if bookingFlow.state}
	<div class="m-4">
		<Card.Root>
			<Card.Header>
				<Card.Title class="flex items-center gap-2">
					<ReceiptTextIcon class="size-5" />
					Booking Page
				</Card.Title>
				<Card.Description>
					Pending booking {bookingFlow.state.booking.bookingIds.join(", ")} is ready for the next step.
				</Card.Description>
			</Card.Header>
			<Card.Content class="space-y-4 text-sm">
				<div class="flex items-center justify-between gap-3">
					<span class="text-muted-foreground">Status</span>
					<span class="font-medium">{bookingFlow.state.booking.status}</span>
				</div>
				<div class="flex items-center justify-between gap-3">
					<span class="text-muted-foreground">Total</span>
					<span class="font-medium">R{bookingFlow.state.booking.totalPrice.toFixed(2)}</span>
				</div>
				<div class="rounded-lg border p-4">
					<div class="mb-3 flex items-center gap-2 font-medium">
						<UsersIcon class="size-4" />
						Players
					</div>
					<div class="space-y-2">
						{#each bookingFlow.state.players as player, index (index)}
							<div class="flex items-center justify-between gap-3 rounded-md border p-3">
								<span>Player {index + 1}</span>
								<span class="text-right">
									{player.name} - {player.email}
								</span>
							</div>
						{/each}
					</div>
				</div>
			</Card.Content>
			<Card.Footer>
				<Button variant="outline" href={confirmHref}>
					<ChevronLeftIcon /> Back
				</Button>
			</Card.Footer>
		</Card.Root>
	</div>
{:else}
	<div class="m-4">
		<Card.Root>
			<Card.Content class="flex flex-col gap-4 p-6 text-sm">
				<div class="flex items-center gap-2 font-medium">
					<CircleAlertIcon class="size-4" />
					No pending booking found
				</div>
				<p class="text-muted-foreground">
					Go back to the confirm step to create a fresh pending booking.
				</p>
				<div>
					<Button variant="outline" href={confirmHref}>
						<ChevronLeftIcon /> Back to confirm
					</Button>
				</div>
			</Card.Content>
		</Card.Root>
	</div>
{/if}
