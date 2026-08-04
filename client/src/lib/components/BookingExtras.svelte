<script lang="ts">
	import type { ExtraBookingDTO } from "$lib/api";
	import { formatCurrency } from "$lib/booking/format";
	import { Card, Table } from "@kayord/ui";

	type Props = {
		extras: ExtraBookingDTO[];
	};

	let { extras }: Props = $props();
</script>

<Card.Root class="overflow-hidden p-0">
	<Table.Root>
		<Table.Header>
			<Table.Row>
				<Table.Head>Extra</Table.Head>
				<Table.Head>Price</Table.Head>
				<Table.Head>Amount</Table.Head>
				<Table.Head>Total</Table.Head>
			</Table.Row>
		</Table.Header>
		<Table.Body>
			{#each extras as extraBooking (extraBooking.extraId)}
				<Table.Row>
					<Table.Cell>{extraBooking.extra.name}</Table.Cell>
					<Table.Cell>{formatCurrency(extraBooking.extra.price)}</Table.Cell>
					<Table.Cell>{extraBooking.amount}</Table.Cell>
					<Table.Cell>{formatCurrency(extraBooking.extra.price * extraBooking.amount)}</Table.Cell>
				</Table.Row>
			{/each}
		</Table.Body>
	</Table.Root>
</Card.Root>
