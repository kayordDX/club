<script lang="ts">
	import { Button, StatusDot } from "@kayord/ui";
	import { type SlotGetAllResponse } from "$lib/api";
	import { Card } from "@kayord/ui";
	import { ChevronRightIcon } from "@lucide/svelte";
	type Props = {
		slots: SlotGetAllResponse[];
	};

	let { slots }: Props = $props();

	const formatTime = (datetime: string) => {
		return new Date(datetime).toLocaleTimeString("en-US", {
			hour: "2-digit",
			minute: "2-digit",
			hour12: false,
		});
	};
</script>

<div class="flex flex-wrap gap-2">
	{#each slots as slot (slot)}
		<Card.Root class="flex w-80 flex-row items-center gap-4 p-4">
			<div class="flex flex-col gap-2">
				<div class="text-muted-foreground text-xs">Tee</div>
				<div class="font-bold">1st</div>
			</div>
			<div class="flex flex-col gap-2">
				<div class="text-muted-foreground text-xs">Start</div>
				<div class="font-bold">{formatTime(slot.startDatetime)}</div>
			</div>
			<div class="flex flex-col gap-2">
				<div class="text-muted-foreground text-xs">End</div>
				<div class="font-bold">{formatTime(slot.endDatetime)}</div>
			</div>
			<div class="flex flex-col justify-center gap-2">
				<div class="text-muted-foreground text-xs">Slots</div>
				<div class="flex items-center gap-1">
					<StatusDot.Root variant="success" />
					<StatusDot.Root variant="success" />
					<StatusDot.Root variant="success" />
					<StatusDot.Root variant="success" />
				</div>
			</div>
			<div class="flex flex-col gap-2">
				<Button size="icon" variant="outline">
					<ChevronRightIcon />
				</Button>
			</div>
		</Card.Root>
	{/each}
</div>
