<script lang="ts">
	import { page } from "$app/state";
	import { Button, Card } from "@kayord/ui";
	import { CircleXIcon } from "@lucide/svelte";

	const transactionId = $derived(page.url.searchParams.get("transactionId") ?? "");
	const error = $derived(page.url.searchParams.get("error") ?? "");
</script>

<div class="mx-auto mt-16 flex max-w-md flex-col items-center gap-6 px-4">
	<Card.Root class="w-full text-center">
		<Card.Content class="flex flex-col items-center gap-4 py-12">
			<CircleXIcon class="size-16 text-red-500" />
			<Card.Title class="text-2xl">Payment Failed</Card.Title>
			<Card.Description>
				Something went wrong while processing your payment.
			</Card.Description>
			{#if error}
				<div class="text-muted-foreground mt-2 max-w-xs text-xs">
					{error}
				</div>
			{/if}
			{#if transactionId}
				<div class="text-muted-foreground text-xs">
					Transaction ID: {transactionId}
				</div>
			{/if}
			<Button href="/bookings" class="mt-4" variant="outline">Back to Bookings</Button>
		</Card.Content>
	</Card.Root>
</div>
