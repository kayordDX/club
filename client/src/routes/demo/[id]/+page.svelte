<script lang="ts">
	import { bookingGet, bookingGetPath } from "$lib/api/booking.remote";
	import { resolve } from "$app/paths";
	import { formatDate, formatTime, formatCurrency } from "$lib/booking/format";
	import { Button, Card } from "@kayord/ui";

	let { data } = $props();

	// Two queries, awaited below — both resolve on the server during SSR.
	// id is stable for this route instance; create each query once.
	// svelte-ignore state_referenced_locally
	const booking = bookingGet(data.id);
	// svelte-ignore state_referenced_locally
	const path = bookingGetPath(data.id);
</script>

<svelte:head>
	<title>Booking #{data.id} — remote function demo</title>
</svelte:head>

<div class="m-4 space-y-4">
	<div class="flex items-center gap-4">
		<Button href={resolve("/demo")} variant="outline" size="sm">← Back</Button>
		<h1 class="text-2xl font-semibold">Booking #{data.id}</h1>
	</div>

	{#await booking}
		<p class="text-muted-foreground">Loading booking…</p>
	{:then b}
		<div class="grid gap-4 md:grid-cols-2">
			<Card.Root>
				<Card.Header><Card.Title>Details</Card.Title></Card.Header>
				<Card.Content class="space-y-2 text-sm">
					<div>Status: <span class="font-medium">{b.bookingStatus?.name ?? "—"}</span></div>
					<div>Paid: {b.isPaid ? "Yes" : "No"}</div>
					<div>Outstanding: {formatCurrency(b.amountOutstanding)}</div>
					<div>Paid so far: {formatCurrency(b.amountPaid)}</div>
					<div>Expires: {formatDate(b.expiresAt)}</div>
				</Card.Content>
			</Card.Root>

			{#await path}
				<Card.Root>
					<Card.Content class="text-muted-foreground py-6 text-sm">Loading facility…</Card.Content>
				</Card.Root>
			{:then p}
				<Card.Root>
					<Card.Header><Card.Title>Facility</Card.Title></Card.Header>
					<Card.Content class="space-y-2 text-sm">
						<div>Outlet: {p.outletName}</div>
						<div>Facility: {p.facilityName}</div>
						<div>When: {formatDate(p.slotStartDatetime)} {formatTime(p.slotStartDatetime)}</div>
					</Card.Content>
				</Card.Root>
			{/await}

			<Card.Root class="md:col-span-2">
				<Card.Header><Card.Title>Players</Card.Title></Card.Header>
				<Card.Content class="space-y-1 text-sm">
					{#each b.slotContractBookings as scb (scb.id)}
						<div>{scb.name ?? scb.userId ?? "—"} — {scb.slotContract.contractName}</div>
					{:else}
						<div class="text-muted-foreground">No players.</div>
					{/each}
				</Card.Content>
			</Card.Root>

			<Card.Root class="md:col-span-2">
				<Card.Header><Card.Title>Extras</Card.Title></Card.Header>
				<Card.Content class="space-y-1 text-sm">
					{#each b.extraBookings as eb (eb.extraId)}
						<div>{eb.extra.name} × {eb.amount} — {formatCurrency(eb.extra.price)}</div>
					{:else}
						<div class="text-muted-foreground">No extras.</div>
					{/each}
				</Card.Content>
			</Card.Root>
		</div>
	{:catch err}
		<p class="text-destructive text-sm">Failed to load booking: {(err as Error).message}</p>
	{/await}
</div>
