<script lang="ts">
	import { bookingGetUser } from "$lib/api/remote/booking.remote";
	import { resolve } from "$app/paths";
	import { formatDate, formatCurrency } from "$lib/booking/format";
	import { Button, Card } from "@kayord/ui";

	let { data } = $props();

	// A `query` — awaited below. Runs on the server during SSR (using the
	// session token from locals), then hydrates on the client with the same value.
	const bookings = bookingGetUser();
</script>

<svelte:head>
	<title>My bookings — remote function demo</title>
</svelte:head>

<div class="m-4 space-y-4">
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-semibold">My bookings</h1>
		<div class="flex gap-2">
			<Button href={resolve("/")} variant="outline">Back to app</Button>
			<Button href="/auth/logout" variant="ghost">Sign out</Button>
		</div>
	</div>

	<p class="text-muted-foreground text-sm">
		Signed in as <strong>{data.user.firstName} {data.user.lastName}</strong> ({data.user.email}). This list was fetched <strong>server-side</strong> via a remote
		function using your session token — no token is present in the browser. View page source to confirm the data is in the HTML.
	</p>

	{#await bookings}
		<p class="text-muted-foreground">Loading…</p>
	{:then result}
		{#if result.items.length === 0}
			<Card.Root>
				<Card.Content class="text-muted-foreground py-8">No bookings yet.</Card.Content>
			</Card.Root>
		{:else}
			<div class="grid gap-3">
				{#each result.items as b (b.id)}
					<Card.Root>
						<Card.Content class="flex items-center justify-between gap-4 py-4">
							<div>
								<div class="font-medium">Booking #{b.id}</div>
								<div class="text-muted-foreground text-sm">
									{b.facilityName ?? "—"} · {formatDate(b.slotStartDatetime)} ·
									{b.playerCount} players
								</div>
							</div>
							<div class="text-right text-sm">
								<div>{b.bookingStatusName}</div>
								<div class="text-muted-foreground">{formatCurrency(b.amountOutstanding)} due</div>
							</div>
							<Button href={resolve(`/demo/${b.id}`)} size="sm">View</Button>
						</Card.Content>
					</Card.Root>
				{/each}
			</div>
		{/if}
	{:catch err}
		<p class="text-destructive text-sm">Failed to load bookings: {(err as Error).message}</p>
	{/await}
</div>
