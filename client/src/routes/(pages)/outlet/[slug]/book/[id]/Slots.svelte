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

<div class="grid grid-cols-[repeat(auto-fit,minmax(22rem,1fr))] place-items-center gap-2">
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
		<Card.Root class="flex flex-row items-center gap-4 p-4">
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
			<div class="flex flex-col gap-2">
				<div class="text-muted-foreground text-xs">Price</div>
				<div class="flex gap-1 font-bold">
					<Popover.Root>
						<Popover.Trigger>
							<button>
								<CircleQuestionMark class="text-primary size-3" />
							</button>
						</Popover.Trigger>
						<Popover.Content>
							<Table.Root>
								<Table.Header>
									<Table.Row>
										<Table.Head>Type</Table.Head>
										<Table.Head>Price</Table.Head>
									</Table.Row>
								</Table.Header>
								<Table.Body>
									<Table.Row>
										<Table.Cell>Member</Table.Cell>
										<Table.Cell>R100.00</Table.Cell>
									</Table.Row>
									<Table.Row>
										<Table.Cell>Non-Member</Table.Cell>
										<Table.Cell>R150.00</Table.Cell>
									</Table.Row>
								</Table.Body>
							</Table.Root>
						</Popover.Content>
					</Popover.Root>
					R100.00
				</div>
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
