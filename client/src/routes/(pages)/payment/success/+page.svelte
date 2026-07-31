<script lang="ts">
	import { page } from "$app/state";
	import { Button, Card } from "@kayord/ui";
	import { CircleCheckIcon } from "@lucide/svelte";

	const transactionId = $derived(page.url.searchParams.get("transactionId") ?? "");
</script>

<div class="mx-auto mt-16 flex max-w-md flex-col items-center gap-6 px-4">
	<Card.Root class="w-full text-center">
		<Card.Content class="flex flex-col items-center gap-4 py-12">
			<CircleCheckIcon class="size-16 text-green-500" />
			<Card.Title class="text-2xl">Payment Successful</Card.Title>
			<Card.Description>Your payment has been processed successfully.</Card.Description>
			{#if transactionId}
				<div class="text-muted-foreground mt-2 text-xs">
					Transaction ID: {transactionId}
				</div>
			{/if}
			<Button href="/bookings" class="mt-4">View My Bookings</Button>
		</Card.Content>
	</Card.Root>
</div>
