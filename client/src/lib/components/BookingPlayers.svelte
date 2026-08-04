<script lang="ts">
	import type { SlotContractBookingDTO } from "$lib/api";
	import { Card, Table } from "@kayord/ui";

	type Props = {
		players: SlotContractBookingDTO[];
	};

	let { players }: Props = $props();
</script>

<Card.Root class="overflow-hidden p-0">
	<Table.Root>
		<Table.Header>
			<Table.Row>
				<Table.Head>Name</Table.Head>
				<Table.Head>Cell No</Table.Head>
				<Table.Head>Email</Table.Head>
			</Table.Row>
		</Table.Header>
		<Table.Body>
			{#each players as player (player.id)}
				<Table.Row>
					<Table.Cell>{player.name}</Table.Cell>
					<Table.Cell>{player.cellphone}</Table.Cell>
					<Table.Cell>{player.email}</Table.Cell>
				</Table.Row>
			{/each}
		</Table.Body>
	</Table.Root>
</Card.Root>
