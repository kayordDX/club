<script lang="ts">
	import { Button, Empty, Popover, StatusDot, Table } from "@kayord/ui";
	import { type SlotGetAllResponse } from "$lib/api";
	import { Card } from "@kayord/ui";
	import { ChevronRightIcon, CircleQuestionMark, TicketXIcon } from "@lucide/svelte";
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
		<Card.Root class="flex w-full flex-row items-center gap-6 p-4">
			<div class="flex flex-col gap-2">
				<div class="text-muted-foreground text-xs">Resource</div>
				<div class="font-bold">
					{slot.resourceName}
				</div>
			</div>
			<div class="flex flex-col gap-2">
				<div class="text-muted-foreground text-xs">Time</div>
				<div class="font-bold">
					{formatTime(slot.startDatetime)}
					<span class="text-muted-foreground">-</span>
					{formatTime(slot.endDatetime)}
				</div>
			</div>
			<div class="flex flex-col gap-2">
				<div class="text-muted-foreground text-xs">Price</div>
				<div class="flex gap-1 font-bold">
					<Popover.Root>
						<Popover.Trigger>
							<button>
								<CircleQuestionMark class="text-primary size-3" />
							</button>
						</Popover.Trigger>
						<Popover.Content class="p-0">
							<Table.Root class="rounded-md">
								<Table.Header>
									<Table.Row>
										<Table.Head>Type</Table.Head>
										<Table.Head>Price</Table.Head>
									</Table.Row>
								</Table.Header>
								<Table.Body>
									<Table.Row>
										<Table.Cell>Price</Table.Cell>
										<Table.Cell>R{slot.price.toFixed(2)}</Table.Cell>
									</Table.Row>
									<Table.Row>
										<Table.Cell>Member</Table.Cell>
										<Table.Cell>R100.00</Table.Cell>
									</Table.Row>
								</Table.Body>
							</Table.Root>
						</Popover.Content>
					</Popover.Root>
					R{slot.price.toFixed(2)}
				</div>
			</div>
			<div class="flex flex-col justify-center gap-2">
				<div class="text-muted-foreground text-xs">Slots</div>
				<div class="flex items-center gap-1">
					<StatusDot.Root variant="error" />
					<StatusDot.Root variant="success" />
					<StatusDot.Root variant="success" />
					<StatusDot.Root variant="success" />
				</div>
			</div>
			<div class="flex flex-col gap-2">
				<Button variant="outline">
					<ChevronRightIcon /> Book
				</Button>
			</div>
		</Card.Root>
	{/each}
</div>
