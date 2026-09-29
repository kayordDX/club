<script lang="ts">
	import { resolve } from "$app/paths";
	import type { ResolvedPathname } from "$app/types";
	import { Button, Card } from "@kayord/ui";
	import type { BookingPaymentDTO } from "$lib/api";
	import { paymentMessage } from "$lib/booking/payments";

	let { balance, payUrl, transactionId = "" }: { balance?: BookingPaymentDTO; payUrl?: ResolvedPathname; transactionId?: string } = $props();
</script>

<Card.Root class="w-full text-center">
	<Card.Content class="flex flex-col items-center gap-4 py-12">
		<Card.Title class="text-2xl">{balance?.isPaid ? "Booking fully paid" : "Payment received"}</Card.Title>
		<Card.Description
			>{balance
				? paymentMessage(balance.isPaid, balance.amountOutstanding)
				: "Check your booking for the latest payment status and remaining balance."}</Card.Description
		>
		{#if transactionId}<p class="text-muted-foreground text-xs">Transaction ID: {transactionId}</p>{/if}
		{#if balance && !balance.isPaid && payUrl}<Button href={payUrl}>Pay remaining balance / choose another method</Button>{/if}
		<Button href={resolve("/bookings")} variant="outline">View My Bookings</Button>
	</Card.Content>
</Card.Root>
