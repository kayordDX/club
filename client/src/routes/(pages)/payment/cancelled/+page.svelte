<script lang="ts">
	import { Button, Card } from "@kayord/ui";
	import { CircleSlashIcon } from "@lucide/svelte";
</script>

<div class="mx-auto mt-16 flex max-w-md flex-col items-center gap-6 px-4">
	<Card.Root class="w-full text-center">
		<Card.Content class="flex flex-col items-center gap-4 py-12">
			<CircleSlashIcon class="size-16 text-amber-500" />
			<Card.Title class="text-2xl">Payment Cancelled</Card.Title>
			<Card.Description>Your payment was cancelled. Your booking is still pending — you can pay again at any time.</Card.Description>
			<Button href="/bookings" class="mt-4" variant="outline">Back to Bookings</Button>
		</Card.Content>
	</Card.Root>
</div>
