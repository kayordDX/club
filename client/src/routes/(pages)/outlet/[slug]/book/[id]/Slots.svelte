<script lang="ts">
	import { Empty } from "@kayord/ui";
	import { type SlotGetAllResponse } from "$lib/api";
	import { TicketXIcon } from "@lucide/svelte";
	import Slot from "./Slot.svelte";
	type Props = {
		slots: SlotGetAllResponse[];
	};

	let { slots }: Props = $props();
</script>

<div class="grid grid-cols-1 place-items-center gap-2">
	{#if slots.length == 0}
		<Empty.Root>
			<Empty.Header>
				<Empty.Media variant="icon">
					<TicketXIcon />
				</Empty.Media>
				<Empty.Title>No Slots Available</Empty.Title>
				<Empty.Description>
					There are no slots available for your current selection
				</Empty.Description>
			</Empty.Header>
			<Empty.Content></Empty.Content>
		</Empty.Root>
	{/if}
	{#each slots as slot (slot)}
		<Slot {slot} />
	{/each}
</div>
